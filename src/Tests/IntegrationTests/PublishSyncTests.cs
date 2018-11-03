using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Handlers;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class PublishSyncTests
    {
        private class Command
        {
            public string Value { get; set; }
        }

        private class Event
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command>
        {
            private readonly ISyncBus _bus;

            public CommandHandler(ISyncBus bus)
            {
                _bus = bus;
            }
            
            public async Task Handle(Command command)
            {
                await Task.Yield();
                _bus.Publish(new Event {Value = command.Value.ToUpper()});
            }
        }
        
        private IContainer _container;
        private IIntegrationTestingBus _bus;
        private ISyncBus _syncBus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c.ConfigureForIntegrationTesting());
            containerBuilder.RegisterHandler<CommandHandler>();
            _container = containerBuilder.Build();

            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
            _syncBus = _container.Resolve<ISyncBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
        
        [Test]
        public async Task PublishesMessage()
        {
            _syncBus.Send(new Command {Value = "Hello World"});

            var command = (Command) _bus.GetPendingMessages().Select(m => m.Body).Single();
            Assert.That(command.Value, Is.EqualTo("Hello World"));
            
            await _bus.ProcessPendingMessages();

            var @event = (Event) _bus.GetPublishedMessages().Select(m => m.Body).Single();
            Assert.That(@event.Value, Is.EqualTo("HELLO WORLD"));
        }
    }
}
