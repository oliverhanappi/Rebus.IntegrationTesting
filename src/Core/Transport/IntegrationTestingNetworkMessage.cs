using System;
using System.Threading;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingNetworkMessage
    {
        private static int _nextId = 1;

        private volatile ITransactionContext _transactionContext;
        private DateTimeOffset _visibleAfter;

        public int Id { get; }

        public DateTimeOffset VisibleAfter => _visibleAfter;

        public TransportMessage TransportMessage { get; }

        public ITransactionContext TransactionContext
        {
            get => _transactionContext;
            set => _transactionContext = value;
        }

        public IntegrationTestingNetworkMessage([NotNull] TransportMessage transportMessage)
        {
            Id = Interlocked.Increment(ref _nextId);
            TransportMessage = transportMessage?.Clone() ?? throw new ArgumentNullException(nameof(transportMessage));
            _visibleAfter = TransportMessage.Headers.TryGetValue(Headers.DeferredUntil, out var deferredUntilHeader)
                ? deferredUntilHeader.ToDateTimeOffset()
                : RebusTime.Now;
        }

        public void DecreaseDeferral(TimeSpan timeSpan)
        {
            _visibleAfter -= timeSpan;

            if (TransportMessage.Headers.ContainsKey(Headers.DeferredUntil))
            {
                TransportMessage.Headers[Headers.DeferredUntil] = _visibleAfter.ToIso8601DateTimeOffset();
            }
        }

        public override string ToString()
        {
            return $"Message #{Id}";
        }
    }
}
