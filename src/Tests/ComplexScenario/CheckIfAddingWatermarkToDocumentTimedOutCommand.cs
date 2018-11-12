using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class CheckIfAddingWatermarkToDocumentTimedOutCommand
    {
        public Guid SagaId { get; }
        [NotNull] public string DocumentId { get; }

        public CheckIfAddingWatermarkToDocumentTimedOutCommand(Guid sagaId, [NotNull] string documentId)
        {
            SagaId = sagaId;
            DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
        }
    }
}
