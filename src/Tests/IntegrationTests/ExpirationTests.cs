using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class ExpirationTests
    {
        private class Command
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command>
        {
            public Task Handle(Command message) => Task.CompletedTask;
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c.ConfigureForIntegrationTesting());
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
        public async Task ProcessesMessageBeforeExpiry()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"}, new Dictionary<string, string>
            {
                {Headers.TimeToBeReceived, "00:00:05"}
            });

            var processedCommand = (Command) _bus.ProcessedMessages.Single();
            Assert.That(processedCommand.Value, Is.EqualTo("Hello World"));
        }

        [Test]
        public async Task DoesNotProcessMessageAfterExpiry()
        {
            await _bus.SendLocal(new Command {Value = "Hello World"}, new Dictionary<string, string>
            {
                {Headers.TimeToBeReceived, "00:00:05"}
            });
            
            _bus.ShiftTime(10_000);
            await _bus.ProcessPendingMessages();

            Assert.That(_bus.ProcessedMessages, Is.Empty);
            Assert.That(_bus.PendingMessages, Is.Empty);
        }
    }
}
