using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.IntegrationTesting.Transport;
using Rebus.IntegrationTesting.Utils;
using Rebus.Messages;
using Rebus.Routing;
using Rebus.Serialization;
using Rebus.Transport;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingBusDecorator : IIntegrationTestingBus, IAdvancedApi, ISyncBus, IWorkersApi 
    {
        private readonly IBus _inner;
        private readonly IntegrationTestingOptions _integrationTestingOptions;
        private readonly IntegrationTestingNetwork _network;
        private readonly ISerializer _serializer;
        private readonly IRouter _router;

        public IAdvancedApi Advanced => this;

        ITopicsApi IAdvancedApi.Topics => _inner.Advanced.Topics;
        IRoutingApi IAdvancedApi.Routing => _inner.Advanced.Routing;
        ITransportMessageApi IAdvancedApi.TransportMessage => _inner.Advanced.TransportMessage;
        IDataBus IAdvancedApi.DataBus => _inner.Advanced.DataBus;
        ISyncBus IAdvancedApi.SyncBus => this;
        IWorkersApi IAdvancedApi.Workers => this;
        
        int IWorkersApi.Count => _inner.Advanced.Workers.Count;

        public IntegrationTestingBusDecorator([NotNull] IBus inner, [NotNull] IntegrationTestingNetwork network,
            [NotNull] ISerializer serializer, [NotNull] IRouter router,
            [NotNull] IntegrationTestingOptions integrationTestingOptions)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _integrationTestingOptions = integrationTestingOptions ?? throw new ArgumentNullException(nameof(integrationTestingOptions));
        }

        public async Task ProcessPendingMessages(CancellationToken cancellationToken = default)
        {
            try
            {
                _network.ResumeReceiving();
                
                using (var maxProcessingTime = new CancellationTokenSource(_integrationTestingOptions.MaxProcessingTime))
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(maxProcessingTime.Token, cancellationToken))
                {
                    await StartWorkers(cts.Token);
                    await _network.WaitUntilQueueIsEmpty(_integrationTestingOptions.InputQueueName, cts.Token);
                    await StopWorkers(cts.Token);
                }
            }
            finally
            {
                _inner.Advanced.Workers.SetNumberOfWorkers(0);
            }
        }

        private async Task StartWorkers(CancellationToken cancellationToken = default)
        {
            _inner.Advanced.Workers.SetNumberOfWorkers(_integrationTestingOptions.NumberOfWorkers);
            await WaitForNumberOfWorkers(_integrationTestingOptions.NumberOfWorkers, cancellationToken);
        }

        private async Task StopWorkers(CancellationToken cancellationToken = default)
        {
            _inner.Advanced.Workers.SetNumberOfWorkers(0);
            await WaitForNumberOfWorkers(0, cancellationToken);
        }

        private async Task WaitForNumberOfWorkers(int expectedCount, CancellationToken cancellationToken = default)
        {
            var millisecondsDelay = 10;
            while (_inner.Advanced.Workers.Count != expectedCount)
            {
                await Task.Delay(millisecondsDelay, cancellationToken);
                millisecondsDelay *= 2;
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

        public async Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            if (AmbientTransactionContext.Current == null)
            {
                var headers = optionalHeaders ?? new Dictionary<string, string>();
                headers[Headers.Intent] = Headers.IntentOptions.PointToPoint;
                
                var message = new Message(headers, commandMessage);
                var isMapped = await _router.IsMapped(message);

                if (!isMapped)
                {
                    await SendLocal(commandMessage, optionalHeaders);
                    return;
                }
            }

            await _inner.Send(commandMessage, AddReturnAddress(optionalHeaders));
        }

        void ISyncBus.Send(object commandMessage, Dictionary<string, string> optionalHeaders)
            => AsyncUtility.RunSync(() => Send(commandMessage, optionalHeaders));

        public Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.SendLocal(commandMessage, AddReturnAddress(optionalHeaders));

        void ISyncBus.SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders)
            => AsyncUtility.RunSync(() => SendLocal(commandMessage, optionalHeaders));

        public Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
            => _inner.Defer(delay, message, optionalHeaders);

        void ISyncBus.Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders)
            => AsyncUtility.RunSync(() => Defer(delay, message, optionalHeaders));
        
        public Task DeferLocal(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
            => _inner.DeferLocal(delay, message, optionalHeaders);

        void ISyncBus.DeferLocal(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders)
            => AsyncUtility.RunSync(() => DeferLocal(delay, message, optionalHeaders));

        public Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.Publish(eventMessage, optionalHeaders);

        void ISyncBus.Publish(object eventMessage, Dictionary<string, string> optionalHeaders)
            => AsyncUtility.RunSync(() => Publish(eventMessage, optionalHeaders));
        
        public Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
            => _inner.Reply(replyMessage, optionalHeaders);

        void ISyncBus.Reply(object replyMessage, Dictionary<string, string> optionalHeaders)
            => AsyncUtility.RunSync(() => Reply(replyMessage, optionalHeaders));

        public Task Subscribe<TEvent>() => _inner.Subscribe<TEvent>();
        void ISyncBus.Subscribe<TEvent>() => AsyncUtility.RunSync(Subscribe<TEvent>);
        public Task Subscribe(Type eventType) => _inner.Subscribe(eventType);
        void ISyncBus.Subscribe(Type eventType) => AsyncUtility.RunSync(() => Subscribe(eventType));
        public Task Unsubscribe<TEvent>() => _inner.Unsubscribe<TEvent>();
        void ISyncBus.Unsubscribe<TEvent>() => AsyncUtility.RunSync(Unsubscribe<TEvent>);
        public Task Unsubscribe(Type eventType) => _inner.Unsubscribe(eventType);
        void ISyncBus.Unsubscribe(Type eventType) => AsyncUtility.RunSync(() => Unsubscribe(eventType));

        public void Dispose() => _inner.Dispose();

        void IWorkersApi.SetNumberOfWorkers(int numberOfWorkers)
        {
            // the number of workers is controlled explicitly by ProcessPendingMessages
        }

        private Dictionary<string, string> AddReturnAddress(Dictionary<string, string> optionalHeaders)
        {
            if (AmbientTransactionContext.Current == null)
            {
                optionalHeaders = optionalHeaders?.Clone() ?? new Dictionary<string, string>();
                optionalHeaders[Headers.ReturnAddress] = _integrationTestingOptions.ReplyQueueName;
            }

            return optionalHeaders;
        }
    }
}
