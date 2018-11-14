using NodaTime;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentProcessingMonitoringEvent
    {
        public Instant MessageTime { get; }
        public string Message { get; }

        public DocumentProcessingMonitoringEvent(Instant messageTime, string message)
        {
            MessageTime = messageTime;
            Message = message;
        }
    }
}
