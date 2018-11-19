using System;
using System.Linq;
using System.Threading;
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
    public class MessageProcessingCancellationTests
    {
        private const string InputQueue = "input";

        private class Message
        {
            public int Value { get; set; }
        }

        private class MessageHandler : IHandleMessages<Message>
        {
            private readonly CancellationTokenSource _cancellationTokenSource;

            [UsedImplicitly]
            public MessageHandler(CancellationTokenSource cancellationTokenSource)
            {
                _cancellationTokenSource = cancellationTokenSource;
            }
            
            public Task Handle(Message message)
            {
                _cancellationTokenSource.Cancel();
                return Task.CompletedTask;
            }
        }
        
        private IContainer _container;
        private IIntegrationTestingBus _bus;
        private CancellationTokenSource _cancellationTokenSource;

        [SetUp]
        public void SetUp()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(_cancellationTokenSource);
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
        public async Task AbortsMessageProcessingWhenCancellationTokenIsCancelled()
        {
            await _bus.SendLocal(new Message {Value = 1});
            await _bus.SendLocal(new Message {Value = 2});

            Assert.That(async () => await _bus.ProcessPendingMessages(_cancellationTokenSource.Token),
                Throws.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());

            var processedMessage = (Message) _bus.ProcessedMessages.Single();
            Assert.That(processedMessage.Value, Is.EqualTo(1));

            var pendingMessage = (Message) _bus.PendingMessages.Single();
            Assert.That(pendingMessage.Value, Is.EqualTo(2));
        }
    }
}
