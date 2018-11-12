using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.DataBus.InMem;
using Rebus.IntegrationTesting.Sagas;
using Rebus.IntegrationTesting.Transport;
using Rebus.IntegrationTesting.Utils;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Routing;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Transport;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingBusDecorator : IIntegrationTestingBus 
    {
        private readonly IBus _inner;
        private readonly IntegrationTestingOptions _integrationTestingOptions;
        private readonly IntegrationTestingNetwork _network;
        private readonly ISerializer _serializer;
        private readonly IRouter _router;
        private readonly ILog _log;
        private readonly IntegrationTestingSagaStorage _sagaStorage;
        private readonly IPipelineInvoker _pipelineInvoker;

        public IAdvancedApi Advanced => _inner.Advanced;
        public InMemDataStore DataBusData { get; }

        public IntegrationTestingBusDecorator(
            [NotNull] IBus inner,
            [NotNull] IntegrationTestingNetwork network,
            [NotNull] ISerializer serializer,
            [NotNull] IRouter router,
            [NotNull] ILog log,
            [NotNull] IntegrationTestingSagaStorage sagaStorage,
            [NotNull] IntegrationTestingOptions integrationTestingOptions,
            [NotNull] InMemDataStore inMemDataStore,
            [NotNull] IPipelineInvoker pipelineInvoker)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _sagaStorage = sagaStorage ?? throw new ArgumentNullException(nameof(sagaStorage));
            _integrationTestingOptions = integrationTestingOptions ?? throw new ArgumentNullException(nameof(integrationTestingOptions));
            _pipelineInvoker = pipelineInvoker ?? throw new ArgumentNullException(nameof(pipelineInvoker));
            DataBusData = inMemDataStore ?? throw new ArgumentNullException(nameof(inMemDataStore));
        }

        public async Task ProcessPendingMessages(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var scope = new RebusTransactionScope())
                {
                    var transportMessage = _network.Receive(_integrationTestingOptions.InputQueueName, scope.TransactionContext);
                    if (transportMessage == null)
                        break;

                    var incomingStepContext = new IncomingStepContext(transportMessage, scope.TransactionContext);
                    await _pipelineInvoker.Invoke(incomingStepContext);

                    await scope.CompleteAsync();
                }
            }
        }

        public void DecreaseDeferral(TimeSpan timeSpan)
        {
            _network.DecreaseDeferral(_integrationTestingOptions.InputQueueName, timeSpan);
        }

        public IReadOnlyList<Message> GetPendingMessages()
            => GetMessages(_integrationTestingOptions.InputQueueName);

        public IReadOnlyList<Message> GetPublishedMessages()
            => GetMessages(_integrationTestingOptions.SubscriberQueueName);

        public IReadOnlyList<Message> GetRepliedMessages()
            => GetMessages(_integrationTestingOptions.ReplyQueueName);

        public IReadOnlyList<Message> GetMessages([NotNull] string queueName)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            return _network.GetMessages(queueName)
                .Select(m => AsyncUtility.RunSync(() => _serializer.Deserialize(m)))
                .ToList();
        }

        public IReadOnlyCollection<ISagaData> GetSagaDatas()
        {
            return _sagaStorage.SagaDatas.ToList();
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
