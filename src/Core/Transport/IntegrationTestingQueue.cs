using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingQueue
    {
        private readonly IntegrationTestingOptions _options;

        private readonly List<IntegrationTestingNetworkMessage> _messages
            = new List<IntegrationTestingNetworkMessage>();

        public IntegrationTestingQueue([NotNull] IntegrationTestingOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Send([NotNull] TransportMessage message, [NotNull] ITransactionContext transactionContext)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            var networkMessage = new IntegrationTestingNetworkMessage(message);

            transactionContext.OnCommitted(() =>
            {
                _messages.Add(networkMessage);
                return Task.CompletedTask;
            });
        }

        [CanBeNull]
        public TransportMessage Receive([NotNull] ITransactionContext transactionContext)
        {
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            var networkMessage = _messages
                .Where(m => m.TransactionContext == null)
                .Where(m => m.VisibleAfter <= RebusTime.Now + _options.DeferralProcessingLimit)
                .Where(m => m.VisibleBefore >= RebusTime.Now)
                .OrderBy(m => m.VisibleAfter)
                .ThenBy(m => m.Id)
                .FirstOrDefault();

            if (networkMessage == null)
                return null;

            networkMessage.TransactionContext = transactionContext;

            transactionContext.OnCommitted(() =>
            {
                _messages.Remove(networkMessage);
                return Task.CompletedTask;
            });

            transactionContext.OnDisposed(() =>
            {
                networkMessage.TransactionContext = null;
            });

            return networkMessage.TransportMessage;
        }

        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<TransportMessage> GetMessages()
        {
            return _messages
                .Where(m => m.TransactionContext == null)
                .Where(m => m.VisibleBefore >= RebusTime.Now)
                .OrderBy(m => m.VisibleAfter)
                .ThenBy(m => m.Id)
                .Select(m => m.TransportMessage.Clone())
                .ToList();
        }

        public void ShiftTime(TimeSpan timeSpan)
        {
            foreach (var message in _messages)
            {
                message.ShiftTime(timeSpan);
            }
        }

        public void Clear()
        {
            _messages.Clear();
        }
    }
}
