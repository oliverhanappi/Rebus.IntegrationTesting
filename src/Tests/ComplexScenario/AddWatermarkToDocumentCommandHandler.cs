using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Retry.Simple;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class AddWatermarkToDocumentCommandHandler :
        IHandleMessages<AddWatermarkToDocumentCommand>,
        IHandleMessages<IFailed<AddWatermarkToDocumentCommand>>
    {
        public const string NoResponseMarker = "DO_NOT_RESPOND";
        public const string ErrorMarker = "FAIL_NOW";

        private readonly IBus _bus;

        [UsedImplicitly]
        public AddWatermarkToDocumentCommandHandler([NotNull] IBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task Handle([NotNull] AddWatermarkToDocumentCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            using (var sourceStream = await _bus.Advanced.DataBus.OpenRead(command.SourceAttachmentId))
            using (var sourceReader = new StreamReader(sourceStream, Encoding.UTF8))
            using (var targetStream = new MemoryStream())
            using (var targetWriter = new StreamWriter(targetStream, new UTF8Encoding(false)))
            {
                var data = await sourceReader.ReadToEndAsync();
                if (data.Contains(NoResponseMarker))
                    return;
                
                if (data.Contains(ErrorMarker))
                    throw new Exception($"Something went wrong: {data}");

                await targetWriter.WriteAsync(data);
                await targetWriter.WriteAsync("WATERMARK");

                await targetWriter.FlushAsync();
                targetStream.Seek(0, SeekOrigin.Begin);

                var attachment = await _bus.Advanced.DataBus.CreateAttachment(targetStream);
                var reply = new WatermarkAddedToDocumentReply(command.RequestId, attachment.Id);

                await _bus.Reply(reply);
            }
        }

        public async Task Handle([NotNull] IFailed<AddWatermarkToDocumentCommand> failure)
        {
            if (failure == null) throw new ArgumentNullException(nameof(failure));

            var requestId = failure.Message.RequestId;
            var failedEvent = new WatermarkAddingToDocumentFailedReply(requestId, failure.ErrorDescription);

            await _bus.Reply(failedEvent);
        }
    }
}
