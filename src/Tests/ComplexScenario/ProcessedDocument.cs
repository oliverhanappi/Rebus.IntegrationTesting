using System;
using System.Globalization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class ProcessedDocument
    {
        [NotNull] public string Id { get; }
        [NotNull] public string SourceAttachmentId { get; }
        public string ProcessedAttachmentId { get; private set; }
        public string ErrorDetails { get; private set; }

        [JsonIgnore] public bool IsProcessed => IsProcessedSuccessfully || IsFailed;
        [JsonIgnore] public bool IsProcessedSuccessfully => ProcessedAttachmentId != null;
        [JsonIgnore] public bool IsFailed => ErrorDetails != null;

        public ProcessedDocument([NotNull] string sourceAttachmentId)
        {
            Id = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
            SourceAttachmentId = sourceAttachmentId ?? throw new ArgumentNullException(nameof(sourceAttachmentId));
        }

        [JsonConstructor]
        protected ProcessedDocument([NotNull] string id, [NotNull] string sourceAttachmentId,
            string processedAttachmentId, string errorDetails)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SourceAttachmentId = sourceAttachmentId ?? throw new ArgumentNullException(nameof(sourceAttachmentId));
            ProcessedAttachmentId = processedAttachmentId;
            ErrorDetails = errorDetails;
        }

        public void MarkAsProcessed([NotNull] string processedAttachmentId)
        {
            if (processedAttachmentId == null) throw new ArgumentNullException(nameof(processedAttachmentId));
            AssertNotProcessed();

            ProcessedAttachmentId = processedAttachmentId;
        }

        public void MarkAsFailed([NotNull] string errorDetails)
        {
            if (errorDetails == null) throw new ArgumentNullException(nameof(errorDetails));
            AssertNotProcessed();

            ErrorDetails = errorDetails;
        }

        private void AssertNotProcessed()
        {
            if (IsProcessed)
                throw new InvalidOperationException($"{this} has already been processed.");
        }

        public override string ToString()
        {
            return $"Document {Id} (src: {SourceAttachmentId}, processed: {IsProcessed} {ProcessedAttachmentId})";
        }
    }
}