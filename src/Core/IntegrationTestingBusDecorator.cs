using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.DataBus.InMem;
using Rebus.IntegrationTesting.Threading;
using Rebus.Pipeline;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Transport;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingBusDecorator : IIntegrationTestingBus
    {
        private readonly IBus _inner;
        private readonly ISerializer _serializer;
        private readonly IPipelineInvoker _pipelineInvoker;
        private readonly IntegrationTestingAsyncTaskFactory _asyncTaskFactory;

        public IntegrationTestingOptions Options { get; }

        public IAdvancedApi Advanced => _inner.Advanced;
        public InMemDataStore DataBusData => Options.DataStore;

        public IMessages PendingMessages { get; }
        public IMessages PublishedMessages { get; }
        public IMessages RepliedMessages { get; }

        private readonly MessageList _overallProcessedMessages;
        public IMessages ProcessedMessages => _overallProcessedMessages;

        public IntegrationTestingBusDecorator(
            [NotNull] IBus inner,
            [NotNull] ISerializer serializer,
            [NotNull] IntegrationTestingOptions options,
            [NotNull] IPipelineInvoker pipelineInvoker,
            [NotNull] IntegrationTestingAsyncTaskFactory asyncTaskFactory)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _pipelineInvoker = pipelineInvoker ?? throw new ArgumentNullException(nameof(pipelineInvoker));
            _asyncTaskFactory = asyncTaskFactory ?? throw new ArgumentNullException(nameof(asyncTaskFactory));

            PendingMessages = GetMessages(Options.InputQueueName);
            PublishedMessages = GetMessages(Options.SubscriberQueueName);
            RepliedMessages = GetMessages(Options.ReplyQueueName);
            
            _overallProcessedMessages = new MessageList(_serializer, this);
        }

        public async Task ProcessPendingMessages(CancellationToken cancellationToken = default)
        {
            var currentlyProcessedMessages = new MessageList(_serializer, this);

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var scope = new RebusTransactionScope())
                {
                    var transportMessage = Options.Network.Receive(Options.InputQueueName, scope.TransactionContext);
                    if (transportMessage == null)
                        break;

                    if (currentlyProcessedMessages.Count >= Options.MaxProcessedMessages)
                        throw new TooManyMessagesProcessedException(currentlyProcessedMessages);

                    scope.TransactionContext.OnCompleted(async () =>
                    {
                        await currentlyProcessedMessages.Add(transportMessage);
                        await _overallProcessedMessages.Add(transportMessage);
                    });

                    var incomingStepContext = new IncomingStepContext(transportMessage, scope.TransactionContext);
                    await _pipelineInvoker.Invoke(incomingStepContext);
                    await scope.CompleteAsync();
                }

                await _asyncTaskFactory.ExecuteDueTasks(cancellationToken);
            }
            
            cancellationToken.ThrowIfCancellationRequested();
        }

        public void ShiftTime(TimeSpan timeSpan)
        {
            Options.Network.ShiftTime(Options.InputQueueName, timeSpan);
        }

        public IMessages GetMessages([NotNull] string queueName)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            var queue = Options.Network.GetQueue(queueName);
            return new MessagesQueueAdapter(queue, _serializer, this);
        }

        public IReadOnlyCollection<ISagaData> GetSagaDatas()
        {
            return Options.SagaStorage.Instances.ToList();
        }

        public Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.Send(commandMessage, optionalHeaders);

        public Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.SendLocal(commandMessage, optionalHeaders);

        public Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
            => _inner.Defer(delay, message, optionalHeaders);

        public Task DeferLocal(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
            => _inner.DeferLocal(delay, message, optionalHeaders);

        public Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.Publish(eventMessage, optionalHeaders);

        public Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.Reply(replyMessage, optionalHeaders);

        public Task Subscribe<TEvent>() => _inner.Subscribe<TEvent>();
        public Task Subscribe(Type eventType) => _inner.Subscribe(eventType);
        public Task Unsubscribe<TEvent>() => _inner.Unsubscribe<TEvent>();
        public Task Unsubscribe(Type eventType) => _inner.Unsubscribe(eventType);

        public void Reset()
        {
            _overallProcessedMessages.Clear();
            Options.Network.Reset();
            Options.DataStore.Reset();
            Options.SubscriberStore.Reset();
            Options.SagaStorage.Reset();
        }

        public void Dispose() => _inner.Dispose();
    }
}
