using System;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentProcessingExecutedEvent
    {
        public int DocumentCount { get; }
        public TimeSpan ProcessingTime { get; }

        public bool IsSuccess { get; }
        public string ErrorDetails { get; }

        public DocumentProcessingExecutedEvent(int documentCount, TimeSpan processingTime,
            bool isSuccess, string errorDetails = null)
        {
            DocumentCount = documentCount;
            ProcessingTime = processingTime;
            IsSuccess = isSuccess;
            ErrorDetails = errorDetails;
        }
    }
}
