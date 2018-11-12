using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class AddWatermarkToDocumentCommand
    {
        [NotNull] public string RequestId { get; }
        [NotNull] public string SourceAttachmentId { get; }

        public AddWatermarkToDocumentCommand([NotNull] string requestId, [NotNull] string sourceAttachmentId)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            SourceAttachmentId = sourceAttachmentId ?? throw new ArgumentNullException(nameof(sourceAttachmentId));
        }
    }
}