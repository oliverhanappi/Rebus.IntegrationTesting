using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentProcessingFailedReply
    {
        [NotNull] public string CorrelationId { get; }
        [NotNull] public string ErrorDetails { get; }

        public DocumentProcessingFailedReply([NotNull] string correlationId, [NotNull] string errorDetails)
        {
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            ErrorDetails = errorDetails ?? throw new ArgumentNullException(nameof(errorDetails));
        }
    }
}