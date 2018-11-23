using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.DataBus.InMem;
using Rebus.IntegrationTesting.Transport;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Transport;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingBusDecorator : IIntegrationTestingBus
    {
        private readonly IBus _inner;
        private readonly IntegrationTestingNetwork _network;
        private readonly ISerializer _serializer;
        private readonly InMemorySagaStorage _sagaStorage;
        private readonly IPipelineInvoker _pipelineInvoker;

        public IntegrationTestingOptions Options { get; }

        public IAdvancedApi Advanced => _inner.Advanced;
        public InMemDataStore DataBusData { get; }

        public IMessages PendingMessages { get; }
        public IMessages PublishedMessages { get; }
        public IMessages RepliedMessages { get; }

        private readonly MessageList _overallProcessedMessages;
        public IMessages ProcessedMessages => _overallProcessedMessages;

        public IntegrationTestingBusDecorator(
            [NotNull] IBus inner,
            [NotNull] IntegrationTestingNetwork network,
            [NotNull] ISerializer serializer,
            [NotNull] InMemorySagaStorage sagaStorage,
            [NotNull] IntegrationTestingOptions options,
            [NotNull] InMemDataStore inMemDataStore,
            [NotNull] IPipelineInvoker pipelineInvoker)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _sagaStorage = sagaStorage ?? throw new ArgumentNullException(nameof(sagaStorage));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _pipelineInvoker = pipelineInvoker ?? throw new ArgumentNullException(nameof(pipelineInvoker));
            DataBusData = inMemDataStore ?? throw new ArgumentNullException(nameof(inMemDataStore));

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
                if (currentlyProcessedMessages.Count >= Options.MaxProcessedMessages)
                    throw new TooManyMessagesProcessedException(currentlyProcessedMessages);
                
                using (var scope = new RebusTransactionScope())
                {
                    var transportMessage = _network.Receive(Options.InputQueueName, scope.TransactionContext);
                    if (transportMessage == null)
                        break;

                    scope.TransactionContext.OnCompleted(async () =>
                    {
                        await currentlyProcessedMessages.Add(transportMessage);
                        await _overallProcessedMessages.Add(transportMessage);
                    });

                    var incomingStepContext = new IncomingStepContext(transportMessage, scope.TransactionContext);
                    await _pipelineInvoker.Invoke(incomingStepContext);
                    await scope.CompleteAsync();
                }
            }
            
            cancellationToken.ThrowIfCancellationRequested();
        }

        public void ShiftTime(TimeSpan timeSpan)
        {
            _network.ShiftTime(Options.InputQueueName, timeSpan);
        }

        public IMessages GetMessages([NotNull] string queueName)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            var queue = _network.GetQueue(queueName);
            return new MessagesQueueAdapter(queue, _serializer, this);
        }

        public IReadOnlyCollection<ISagaData> GetSagaDatas()
        {
            return _sagaStorage.Instances.ToList();
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

        public void Dispose() => _inner.Dispose();
    }
}
