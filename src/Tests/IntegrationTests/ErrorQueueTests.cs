using System;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Retry.Simple;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class ErrorQueueTests
    {
        private class Command
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command>
        {
            public Task Handle(Command command)
            {
                throw new Exception("oops");
            }
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c
                .Options(o => o.SimpleRetryStrategy("Errors"))
                .ConfigureForIntegrationTesting());

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
        public async Task SavesMessageInErrorQueue()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});

            var messages = _bus.GetMessages("Errors");
            
            Assert.That(messages, Has.Count.EqualTo(1));
            Assert.That(((Command) messages[0]).Value, Is.EqualTo("Hello World"));
            Assert.That(messages.Headers[0][Headers.ErrorDetails], Does.Contain("oops"));
        }
    }
}
