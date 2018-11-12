using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class WatermarkAddingToDocumentFailedReply
    {
        [NotNull] public string RequestId { get; }
        [NotNull] public string ErrorDetails { get; }

        public WatermarkAddingToDocumentFailedReply([NotNull] string requestId, [NotNull] string errorDetails)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            ErrorDetails = errorDetails ?? throw new ArgumentNullException(nameof(errorDetails));
        }
    }
}
