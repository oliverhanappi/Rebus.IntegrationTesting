using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class SendTests
    {
        private class IncomingCommand
        {
            public string Value { get; set; }
        }

        private class OutgoingCommand
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<IncomingCommand>
        {
            private readonly IBus _bus;

            public CommandHandler(IBus bus)
            {
                _bus = bus;
            }

            public async Task Handle(IncomingCommand incomingCommand)
            {
                await Task.Yield();
                await _bus.Send(new OutgoingCommand {Value = incomingCommand.Value.ToUpper()});
            }
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c
                .ConfigureForIntegrationTesting()
                .Routing(r => r.TypeBased().Map<OutgoingCommand>("OtherQueue")));

            containerBuilder.RegisterHandler<CommandHandler>();
            _container = containerBuilder.Build();

            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }

        [Test]
        public async Task SendsMessage()
        {
            await _bus.Send(new IncomingCommand {Value = "Hello World"});

            var incomingCommand = (IncomingCommand) _bus.PendingMessages.Single();
            Assert.That(incomingCommand.Value, Is.EqualTo("Hello World"));

            await _bus.ProcessPendingMessages();

            var outgoingCommand = (OutgoingCommand) _bus.GetMessages("OtherQueue").Single();
            Assert.That(outgoingCommand.Value, Is.EqualTo("HELLO WORLD"));
        }
    }
}
