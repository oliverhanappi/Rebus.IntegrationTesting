using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Transactions;
using Rebus.Messages;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingNetwork
    {
        private readonly ConcurrentDictionary<string, IntegrationTestingQueue> _queues
            = new ConcurrentDictionary<string, IntegrationTestingQueue>(StringComparer.OrdinalIgnoreCase);

        [NotNull]
        private IntegrationTestingQueue GetQueue([NotNull] string queueName)
        {
            return _queues.GetOrAdd(queueName, _ => new IntegrationTestingQueue());
        }
        
        [NotNull]
        public IReadOnlyList<TransportMessage> GetMessages([NotNull] string queueName)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            return GetQueue(queueName).GetMessages();
        }

        public void Send([NotNull] string queueName, [NotNull] TransportMessage message,
            [NotNull] IntegrationTestingTransaction transaction)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var queue = GetQueue(queueName);
            queue.Send(message, transaction);
        }

        [CanBeNull]
        public TransportMessage Receive([NotNull] string queueName,
            [NotNull] IntegrationTestingTransaction transaction)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var queue = GetQueue(queueName);
            return queue.Receive(transaction);
        }

        public Task WaitUntilQueueIsEmpty([NotNull] string queueName, CancellationToken cancellationToken = default)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            var queue = GetQueue(queueName);
            return queue.WaitUntilEmpty(cancellationToken);
        }
    }
}
