using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class SendLocalSyncTests
    {
        private class Command1
        {
            public string Value { get; set; }
        }

        private class Command2
        {
            public string Value { get; set; }
        }

        private class Event
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command1>, IHandleMessages<Command2>
        {
            private readonly ISyncBus _bus;

            public CommandHandler(ISyncBus bus)
            {
                _bus = bus;
            }

            public async Task Handle(Command1 command1)
            {
                await Task.Yield();
                _bus.SendLocal(new Command2 {Value = command1.Value.ToUpper()});
            }

            public async Task Handle(Command2 command2)
            {
                await Task.Yield();
                _bus.Publish(new Event {Value = command2.Value + "Reply"});
            }
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;
        private ISyncBus _syncBus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c
                .ConfigureForIntegrationTesting()
                .Routing(r => r.TypeBased().Map<Command2>("OtherQueue")));

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
        public async Task SendsMessageLocally()
        {
            _syncBus.Send(new Command1 {Value = "Hello World"});

            var incomingCommand = (Command1) _bus.PendingMessages.Single();
            Assert.That(incomingCommand.Value, Is.EqualTo("Hello World"));

            await _bus.ProcessPendingMessages();

            var @event = (Event) _bus.PublishedMessages.Single();
            Assert.That(@event.Value, Is.EqualTo("HELLO WORLDReply"));
        }
    }
}
