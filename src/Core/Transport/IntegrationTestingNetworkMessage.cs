using System;
using System.Threading;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.IntegrationTesting.Transactions;
using Rebus.Messages;
using Rebus.Time;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingNetworkMessage
    {
        private static int _nextId = 1;

        public int Id { get; }
        public DateTimeOffset VisibleAfter { get; }
        public TransportMessage TransportMessage { get; }

        private volatile IntegrationTestingTransaction _transaction;

        public IntegrationTestingTransaction Transaction
        {
            get => _transaction;
            set => _transaction = value;
        }

        public IntegrationTestingNetworkMessage([NotNull] TransportMessage transportMessage)
        {
            Id = Interlocked.Increment(ref _nextId);
            TransportMessage = transportMessage?.Clone() ?? throw new ArgumentNullException(nameof(transportMessage));
            VisibleAfter = TransportMessage.Headers.TryGetValue(Headers.DeferredUntil, out var deferredUntilHeader)
                ? deferredUntilHeader.ToDateTimeOffset()
                : RebusTime.Now;
        }

        public override string ToString()
        {
            return $"Message #{Id}";
        }
    }
}
