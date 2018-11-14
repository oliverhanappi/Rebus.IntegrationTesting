using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Sagas;
using Rebus.Time;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentProcessingSaga : Saga<DocumentProcessingSagaData>,
        IAmInitiatedBy<ProcessDocumentsCommand>,
        IHandleMessages<WatermarkAddedToDocumentReply>,
        IHandleMessages<WatermarkAddingToDocumentFailedReply>,
        IHandleMessages<CheckIfAddingWatermarkToDocumentTimedOutCommand>
    {
        private static class RequestId
        {
            public static string Create(Guid sagaId, string documentId) => $"{sagaId:D}_{documentId}";
            public static Guid GetSagaId(string requestId) => Guid.Parse(requestId.Split(new[] {'_'}, 2)[0]);
            public static string GetDocumentId(string requestId) => requestId.Split(new[] {'_'}, 2)[1];
        }

        private readonly IBus _bus;
        private readonly IMessageContext _messageContext;
        private readonly IClock _clock;
        private readonly DocumentProcessingOptions _options;

        [UsedImplicitly]
        public DocumentProcessingSaga([NotNull] IBus bus, [NotNull] IMessageContext messageContext,
            [NotNull] IClock clock, [NotNull] DocumentProcessingOptions options)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _messageContext = messageContext ?? throw new ArgumentNullException(nameof(messageContext));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected override void CorrelateMessages(ICorrelationConfig<DocumentProcessingSagaData> config)
        {
            config.Correlate<ProcessDocumentsCommand>(c => c.CorrelationId, d => d.CorrelationId);
            config.Correlate<WatermarkAddedToDocumentReply>(e => RequestId.GetSagaId(e.RequestId), d => d.Id);
            config.Correlate<WatermarkAddingToDocumentFailedReply>(e => RequestId.GetSagaId(e.RequestId), d => d.Id);
            config.Correlate<CheckIfAddingWatermarkToDocumentTimedOutCommand>(c => c.SagaId, d => d.Id);
        }

        public async Task Handle([NotNull] ProcessDocumentsCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!IsNew)
                throw new InvalidOperationException("Saga is not new.");

            var processedDocuments = new List<ProcessedDocument>();

            foreach (var documentAttachmentId in command.DocumentAttachmentIds)
            {
                var processedDocument = new ProcessedDocument(documentAttachmentId);
                var requestId = RequestId.Create(Data.Id, processedDocument.Id);

                processedDocuments.Add(processedDocument);
                await _bus.Send(new AddWatermarkToDocumentCommand(requestId, documentAttachmentId));

                var checkTimeoutCommand =
                    new CheckIfAddingWatermarkToDocumentTimedOutCommand(Data.Id, processedDocument.Id);

                await _bus.DeferLocal(_options.WatermarkAddingTimeout, checkTimeoutCommand);
            }

            Data.CorrelationId = command.CorrelationId;
            Data.ReplyAddress = _messageContext.Headers[Headers.ReturnAddress];
            Data.Documents = processedDocuments;
            Data.StartTime = _clock.GetCurrentInstant();
        }

        public async Task Handle([NotNull] WatermarkAddedToDocumentReply reply)
        {
            if (reply == null) throw new ArgumentNullException(nameof(reply));

            var documentId = RequestId.GetDocumentId(reply.RequestId);
            var processedDocument = Data.Documents.Single(d => d.Id == documentId);

            processedDocument.MarkAsProcessed(reply.ProcessedAttachmentId);

            await TryCompleteSaga();
        }

        public async Task Handle([NotNull] WatermarkAddingToDocumentFailedReply reply)
        {
            if (reply == null) throw new ArgumentNullException(nameof(reply));

            var documentId = RequestId.GetDocumentId(reply.RequestId);
            var processedDocument = Data.Documents.Single(d => d.Id == documentId);

            processedDocument.MarkAsFailed(reply.ErrorDetails);

            await TryCompleteSaga();
        }

        public async Task Handle([NotNull] CheckIfAddingWatermarkToDocumentTimedOutCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var document = Data.Documents.Single(d => d.Id == command.DocumentId);
            document.MarkAsFailed("Processing timed out.");

            await TryCompleteSaga();
        }

        private async Task TryCompleteSaga()
        {
            if (Data.Documents.All(d => d.IsProcessed))
            {
                var isFailed = Data.Documents.Any(d => d.IsFailed);
                string failureDetails = null;

                if (isFailed)
                {
                    var errors = Data.Documents
                        .Where(d => d.IsFailed)
                        .Select(d => $"{d.SourceAttachmentId}: {d.ErrorDetails}")
                        .ToList();

                    failureDetails = String.Join(Environment.NewLine, errors);

                    var failedReply = new DocumentProcessingFailedReply(Data.CorrelationId, failureDetails);
                    await _bus.Advanced.Routing.Send(Data.ReplyAddress, failedReply);

                    await _bus.Publish(new DocumentProcessingMonitoringEvent(
                        _clock.GetCurrentInstant(), $"Processing of {Data.Documents.Count} documents failed."));
                }
                else
                {
                    var mergeResultAttachmentId = await MergeDocuments();
                    var processedReply = new DocumentsProcessedReply(Data.CorrelationId, mergeResultAttachmentId);

                    await _bus.Advanced.Routing.Send(Data.ReplyAddress, processedReply);

                    var duration = _clock.GetCurrentInstant() - Data.StartTime;
                    var message = $"Processing of {Data.Documents.Count} documents finished " +
                                  $"after {duration.TotalMilliseconds:n0} ms.";

                    await _bus.Publish(new DocumentProcessingMonitoringEvent(_clock.GetCurrentInstant(), message));
                }

                MarkAsComplete();
            }
        }

        private async Task<string> MergeDocuments()
        {
            using (var mergedStream = new MemoryStream())
            using (var mergedWriter = new StreamWriter(mergedStream, new UTF8Encoding(false)))
            {
                foreach (var processedDocument in Data.Documents)
                {
                    if (!processedDocument.IsProcessedSuccessfully)
                        throw new InvalidOperationException($"{processedDocument} was not processed successfully.");

                    using (var documentStream =
                        await _bus.Advanced.DataBus.OpenRead(processedDocument.ProcessedAttachmentId))
                    using (var documentReader = new StreamReader(documentStream, Encoding.UTF8))
                    {
                        var data = await documentReader.ReadToEndAsync();
                        await mergedWriter.WriteAsync(data);
                    }
                }

                await mergedWriter.FlushAsync();

                mergedStream.Seek(0, SeekOrigin.Begin);
                var attachment = await _bus.Advanced.DataBus.CreateAttachment(mergedStream);

                return attachment.Id;
            }
        }
    }
}
