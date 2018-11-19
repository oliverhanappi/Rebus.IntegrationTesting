using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class PublishTests
    {
        private const string InputQueue = "input";
        private const string ReceivedEventsQueue = "received-events";
        
        private class Command
        {
            public string Value { get; set; }
        }

        private class Event
        {
            public string Value { get; set; }
        }

        private class CommandHandler : IHandleMessages<Command>
        {
            private readonly IBus _bus;

            [UsedImplicitly]
            public CommandHandler(IBus bus)
            {
                _bus = bus;
            }
            
            public async Task Handle(Command command)
            {
                await Task.Yield();
                await _bus.Publish(new Event {Value = command.Value.ToUpper()});
            }
        }

        private class EventHandler : IHandleMessages<Event>
        {
            private readonly IBus _bus;

            [UsedImplicitly]
            public EventHandler(IBus bus)
            {
                _bus = bus;
            }
            
            public async Task Handle(Event message)
            {
                await _bus.Advanced.TransportMessage.Forward(ReceivedEventsQueue);
            }
        }
        
        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c.ConfigureForIntegrationTesting(b => b.InputQueueName(InputQueue)));
            containerBuilder.RegisterHandler<CommandHandler>();
            containerBuilder.RegisterHandler<EventHandler>();
            _container = containerBuilder.Build();

            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
        
        [Test]
        public async Task PublishesMessage()
        {
            await _bus.Send(new Command {Value = "Hello World"});

            var command = (Command) _bus.PendingMessages.Single();
            Assert.That(command.Value, Is.EqualTo("Hello World"));
            
            await _bus.ProcessPendingMessages();

            var @event = (Event) _bus.PublishedMessages.Single();
            Assert.That(@event.Value, Is.EqualTo("HELLO WORLD"));
        }

        [Test]
        public async Task DoesNotSendPublishedMessagesToSelfByDefault()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});
            Assert.That(_bus.GetMessages(ReceivedEventsQueue), Is.Empty);
        }

        [Test]
        public async Task SendsPublishedMessagesToSelfAfterSubscribing()
        {
            await _bus.Subscribe<Event>();
            await _bus.ProcessPendingMessages();
            
            await _bus.ProcessMessage(new Command {Value = "Hello World"});

            var @event = (Event) _bus.GetMessages(ReceivedEventsQueue).Single();
            Assert.That(@event.Value, Is.EqualTo("HELLO WORLD"));
        }
    }
}
