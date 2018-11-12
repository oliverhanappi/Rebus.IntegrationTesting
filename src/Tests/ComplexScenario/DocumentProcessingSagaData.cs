using System;
using System.Collections.Generic;
using Rebus.Sagas;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentProcessingSagaData : SagaData
    {
        public string CorrelationId { get; set; }
        public string ReplyAddress { get; set; }
        public IReadOnlyCollection<ProcessedDocument> Documents { get; set; }

        public DateTimeOffset StartTime { get; set; }
    }
}
