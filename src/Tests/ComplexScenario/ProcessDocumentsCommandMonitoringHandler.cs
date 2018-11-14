using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using Rebus.Bus;
using Rebus.Handlers;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class ProcessDocumentsCommandMonitoringHandler : IHandleMessages<ProcessDocumentsCommand>
    {
        private readonly IBus _bus;
        private readonly IClock _clock;

        [UsedImplicitly]
        public ProcessDocumentsCommandMonitoringHandler([NotNull] IBus bus, [NotNull] IClock clock)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }
        
        public async Task Handle(ProcessDocumentsCommand command)
        {
            var message = $"Processing of {command.DocumentAttachmentIds.Count} documents started.";
            await _bus.Publish(new DocumentProcessingMonitoringEvent(_clock.GetCurrentInstant(), message));
        }
    }
}
