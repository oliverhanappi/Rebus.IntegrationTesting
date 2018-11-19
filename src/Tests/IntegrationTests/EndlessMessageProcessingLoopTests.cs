using System.Linq;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class EndlessMessageProcessingLoopTests
    {
        private const string InputQueue = "input";

        private class Message
        {
            public string Value { get; set; }
        }

        private class MessageHandler : IHandleMessages<Message>
        {
            private readonly IBus _bus;

            public MessageHandler(IBus bus)
            {
                _bus = bus;
            }
            
            public Task Handle(Message message)
            {
                return _bus.Advanced.TransportMessage.Forward(InputQueue);
            }
        }
        
        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c
                .ConfigureForIntegrationTesting(b => b.InputQueueName(InputQueue).MaxProcessedMessages(2)));
            containerBuilder.RegisterHandler<MessageHandler>();
            _container = containerBuilder.Build();

            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
        
        [Test]
        public void AbortsMessageProcessingAfterReachingMaxProcessingTime()
        {
            var message = new Message {Value = "Hello World"};
            var exception = Assert.ThrowsAsync<TooManyMessagesProcessedException>(
                async () => await _bus.ProcessMessage(message));

            Assert.That(exception.ProcessedMessages, Has.Count.EqualTo(2));
            Assert.That(exception.ProcessedMessages.Select(m => ((Message) m.Body).Value),
                Is.All.EqualTo("Hello World"));
        }
    }
}
