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

        private DateTimeOffset _visibleAfter;
        private DateTimeOffset _visibleBefore;

        public int Id { get; }

        public DateTimeOffset VisibleAfter => _visibleAfter;
        public DateTimeOffset VisibleBefore => _visibleBefore;

        public TransportMessage TransportMessage { get; }

        public ITransactionContext TransactionContext { get; set; }

        public IntegrationTestingNetworkMessage([NotNull] TransportMessage transportMessage)
        {
            if (transportMessage == null)
                throw new ArgumentNullException(nameof(transportMessage));
            
            Id = Interlocked.Increment(ref _nextId);
            TransportMessage = transportMessage.Clone();
            _visibleAfter = TransportMessage.Headers.TryGetValue(Headers.DeferredUntil, out var deferredUntilHeader)
                ? deferredUntilHeader.ToDateTimeOffset()
                : RebusTime.Now;

            var sentTime = TransportMessage.Headers.TryGetValue(Headers.SentTime, out var sentTimeHeader)
                ? sentTimeHeader.ToDateTimeOffset()
                : RebusTime.Now;

            var timeToBeReceived = TransportMessage.Headers.TryGetValue(Headers.TimeToBeReceived, out var timeToBeReceivedHeader)
                ? TimeSpan.Parse(timeToBeReceivedHeader)
                : TimeSpan.FromTicks(-1);

            _visibleBefore = timeToBeReceived > TimeSpan.Zero ? sentTime + timeToBeReceived : DateTimeOffset.MaxValue;
        }

        public void ShiftTime(TimeSpan timeSpan)
        {
            _visibleAfter -= timeSpan;
            _visibleBefore -= timeSpan;

            if (TransportMessage.Headers.ContainsKey(Headers.DeferredUntil))
            {
                TransportMessage.Headers[Headers.DeferredUntil] = _visibleAfter.ToIso8601DateTimeOffset();
            }

            if (TransportMessage.Headers.ContainsKey(Headers.SentTime))
            {
                var sentTime = TransportMessage.Headers[Headers.SentTime].ToDateTimeOffset();
                sentTime -= timeSpan;

                TransportMessage.Headers[Headers.SentTime] = sentTime.ToIso8601DateTimeOffset();
            }
        }

        public override string ToString()
        {
            return $"Message #{Id}";
        }
    }
}
