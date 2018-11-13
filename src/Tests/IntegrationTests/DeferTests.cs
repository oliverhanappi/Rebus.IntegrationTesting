using System;
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
    public class DeferTests
    {
        private class Command
        {
            public string Value { get; set; }
            public int Delay { get; set; }
        }

        private class DeferredCommand
        {
            public string Value { get; set; }
        }

        private class OutgoingCommand
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command>, IHandleMessages<DeferredCommand>
        {
            private readonly IBus _bus;

            public CommandHandler(IBus bus)
            {
                _bus = bus;
            }

            public async Task Handle(Command command)
            {
                await Task.Yield();

                var deferredCommand = new DeferredCommand {Value = command.Value.ToUpper()};
                await _bus.DeferLocal(TimeSpan.FromMilliseconds(command.Delay), deferredCommand);
            }

            public async Task Handle(DeferredCommand command)
            {
                await Task.Yield();
                await _bus.Send(new OutgoingCommand {Value = command.Value + " Deferred"});
            }
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c
                .ConfigureForIntegrationTesting(i => i.DeferralProcessingLimit(1_000))
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
        public async Task DeferWithDelayLessThanLimit_ProcessesMessage()
        {
            await _bus.Send(new Command {Value = "Hello World", Delay = 200});

            var incomingCommand = (Command) _bus.PendingMessages.Single();
            Assert.That(incomingCommand.Value, Is.EqualTo("Hello World"));

            await _bus.ProcessPendingMessages();

            var outgoingCommand = (OutgoingCommand) _bus.GetMessages("OtherQueue").Single();
            Assert.That(outgoingCommand.Value, Is.EqualTo("HELLO WORLD Deferred"));
        }

        [Test]
        public async Task DeferWithDelayGreaterThanLimit_DoesNotProcessMessage()
        {
            await _bus.Send(new Command {Value = "Hello World", Delay = 5_000});

            var incomingCommand = (Command) _bus.PendingMessages.Single();
            Assert.That(incomingCommand.Value, Is.EqualTo("Hello World"));

            await _bus.ProcessPendingMessages();

            var deferredCommand1 = (DeferredCommand) _bus.PendingMessages.Single();
            Assert.That(deferredCommand1.Value, Is.EqualTo("HELLO WORLD"));

            Assert.That(_bus.GetMessages("OtherQueue"), Is.Empty);

            await _bus.ProcessPendingMessages();

            var deferredCommand2 = (DeferredCommand) _bus.PendingMessages.Single();
            Assert.That(deferredCommand2.Value, Is.EqualTo("HELLO WORLD"));

            Assert.That(_bus.GetMessages("OtherQueue"), Is.Empty);
            
            _bus.DecreaseDeferral(5_000);
            await _bus.ProcessPendingMessages();

            Assert.That(_bus.PendingMessages, Is.Empty);

            var outgoingCommand = (OutgoingCommand) _bus.GetMessages("OtherQueue").Single();
            Assert.That(outgoingCommand.Value, Is.EqualTo("HELLO WORLD Deferred"));
        }
    }
}
