using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingNetwork
    {
        private readonly TimeSpan _deferralProcessingLimit;

        private readonly ConcurrentDictionary<string, IntegrationTestingQueue> _queues
            = new ConcurrentDictionary<string, IntegrationTestingQueue>(StringComparer.OrdinalIgnoreCase);

        public IntegrationTestingNetwork(TimeSpan deferralProcessingLimit)
        {
            _deferralProcessingLimit = deferralProcessingLimit;
        }
        
        [NotNull]
        public IntegrationTestingQueue GetQueue([NotNull] string queueName)
        {
            return _queues.GetOrAdd(queueName, _ => new IntegrationTestingQueue(_deferralProcessingLimit));
        }
        
        public void Send([NotNull] string queueName, [NotNull] TransportMessage message,
            [NotNull] ITransactionContext transactionContext)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            var queue = GetQueue(queueName);
            queue.Send(message, transactionContext);
        }

        [CanBeNull]
        public TransportMessage Receive([NotNull] string queueName, [NotNull] ITransactionContext transactionContext)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            var queue = GetQueue(queueName);
            return queue.Receive(transactionContext);
        }

        public void ShiftTime([NotNull] string queueName, TimeSpan timeSpan)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            GetQueue(queueName).ShiftTime(timeSpan);
        }
    }
}
