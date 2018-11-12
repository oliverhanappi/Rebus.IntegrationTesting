using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class ProcessDocumentsCommand
    {
        [NotNull] public string CorrelationId { get; }
        [NotNull] public IReadOnlyCollection<string> DocumentAttachmentIds { get; }

        public ProcessDocumentsCommand([NotNull] string correlationId,
            [NotNull] IReadOnlyCollection<string> documentAttachmentIds)
        {
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            DocumentAttachmentIds = documentAttachmentIds ??
                                    throw new ArgumentNullException(nameof(documentAttachmentIds));
        }
    }
}