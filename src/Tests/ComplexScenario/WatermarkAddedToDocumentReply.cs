using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class WatermarkAddedToDocumentReply
    {
        [NotNull] public string RequestId { get; }
        [NotNull] public string ProcessedAttachmentId { get; }

        public WatermarkAddedToDocumentReply([NotNull] string requestId, [NotNull] string processedAttachmentId)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            ProcessedAttachmentId = processedAttachmentId ??
                                    throw new ArgumentNullException(nameof(processedAttachmentId));
        }
    }
}
