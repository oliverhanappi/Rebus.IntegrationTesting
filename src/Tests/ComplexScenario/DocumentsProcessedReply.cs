using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentsProcessedReply
    {
        [NotNull] public string CorrelationId { get; }
        [NotNull] public string ResultAttachmentId { get; }

        public DocumentsProcessedReply([NotNull] string correlationId, [NotNull] string resultAttachmentId)
        {
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            ResultAttachmentId = resultAttachmentId ?? throw new ArgumentNullException(nameof(resultAttachmentId));
        }
    }
}
