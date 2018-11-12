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

        private readonly List<TaskCompletionSource<object>> _taskCompletionSources
            = new List<TaskCompletionSource<object>>();

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
                lock (_messages)
                {
                    _messages.Add(networkMessage);
                }

                return Task.CompletedTask;
            });
        }

        [CanBeNull]
        public TransportMessage Receive([NotNull] ITransactionContext transactionContext)
        {
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            lock (_messages)
            {
                var networkMessage = _messages
                    .Where(m => m.TransactionContext == null)
                    .Where(m => m.VisibleAfter <= RebusTime.Now + _options.DeferralProcessingLimit)
                    .OrderBy(m => m.VisibleAfter)
                    .ThenBy(m => m.Id)
                    .FirstOrDefault();

                if (networkMessage == null)
                    return null;

                networkMessage.TransactionContext = transactionContext;

                transactionContext.OnCommitted(() =>
                {
                    lock (_messages)
                    {
                        _messages.Remove(networkMessage);
                    }

                    return Task.CompletedTask;
                });

                transactionContext.OnDisposed(() =>
                {
                    lock (_messages)
                    {
                        networkMessage.TransactionContext = null;
                    }
                });

                return networkMessage.TransportMessage;
            }
        }

        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<TransportMessage> GetMessages()
        {
            lock (_messages)
            {
                return _messages
                    .Where(m => m.TransactionContext == null)
                    .OrderBy(m => m.VisibleAfter)
                    .ThenBy(m => m.Id)
                    .Select(m => m.TransportMessage.Clone())
                    .ToList();
            }
        }

        public void DecreaseDeferral(TimeSpan timeSpan)
        {
            lock (_messages)
            {
                foreach (var message in _messages)
                {
                    message.DecreaseDeferral(timeSpan);
                }
            }
        }
    }
}
