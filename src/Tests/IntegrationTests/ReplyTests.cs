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
    public class ReplyTests
    {
        private class Command
        {
            public string Value { get; set; }
        }

        private class Reply
        {
            public string Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command>
        {
            private readonly IBus _bus;

            public CommandHandler(IBus bus)
            {
                _bus = bus;
            }
            
            public async Task Handle(Command command)
            {
                await Task.Yield();
                await _bus.Reply(new Reply {Value = command.Value.ToUpper()});
            }
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
        public async Task ReceivesReply()
        {
            await _bus.Send(new Command {Value = "Hello World"});

            var command = (Command) _bus.GetPendingMessages().Select(m => m.Body).Single();
            Assert.That(command.Value, Is.EqualTo("Hello World"));
            
            await _bus.ProcessPendingMessages();

            var reply = (Reply) _bus.GetRepliedMessages().Select(m => m.Body).Single();
            Assert.That(reply.Value, Is.EqualTo("HELLO WORLD"));
        }
    }
}
