using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.IntegrationTesting.Transactions;
using Rebus.Messages;
using Rebus.Time;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingQueue
    {
        private readonly IntegrationTestingOptions _options;

        private readonly List<IntegrationTestingNetworkMessage> _messages
            = new List<IntegrationTestingNetworkMessage>();

        private readonly List<TaskCompletionSource<object>> _taskCompletionSources
            = new List<TaskCompletionSource<object>>();

        private volatile bool _receivingPaused;

        public IntegrationTestingQueue([NotNull] IntegrationTestingOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Send([NotNull] TransportMessage message, [NotNull] IntegrationTestingTransaction transaction)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var networkMessage = new IntegrationTestingNetworkMessage(message);

            transaction.OnCommit(() =>
            {
                lock (_messages)
                {
                    _messages.Add(networkMessage);
                }
            });
        }

        [CanBeNull]
        public TransportMessage Receive([NotNull] IntegrationTestingTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            if (Monitor.TryEnter(_messages, TimeSpan.Zero))
            {
                try
                {
                    if (_receivingPaused)
                        return null;

                    var networkMessage = _messages
                        .Where(m => m.Transaction == null)
                        .Where(m => m.VisibleAfter <= RebusTime.Now)
                        .OrderBy(m => m.VisibleAfter)
                        .ThenBy(m => m.Id)
                        .FirstOrDefault();

                    if (networkMessage == null)
                        return null;

                    networkMessage.Transaction = transaction;

                    transaction.OnCommit(() =>
                    {
                        lock (_messages)
                        {
                            _messages.Remove(networkMessage);
                        }
                    });

                    transaction.OnDispose(() =>
                    {
                        lock (_messages)
                        {
                            networkMessage.Transaction = null;

                            if (IsQueueEmpty())
                            {
                                _receivingPaused = true;

                                foreach (var taskCompletionSource in _taskCompletionSources)
                                {
                                    taskCompletionSource.TrySetResult(null);
                                }

                                _taskCompletionSources.Clear();
                            }
                        }
                    });

                    return networkMessage.TransportMessage;
                }
                finally
                {
                    Monitor.Exit(_messages);
                }
            }

            return null;
        }

        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<TransportMessage> GetMessages()
        {
            lock (_messages)
            {
                return _messages
                    .Where(m => m.Transaction == null)
                    .OrderBy(m => m.VisibleAfter)
                    .ThenBy(m => m.Id)
                    .Select(m => m.TransportMessage.Clone())
                    .ToList();
            }
        }

        [NotNull]
        public Task WaitUntilEmpty(CancellationToken cancellationToken = default)
        {
            lock (_messages)
            {
                if (IsQueueEmpty())
                    return Task.CompletedTask;

                var taskCompletionSource = new TaskCompletionSource<object>();
                cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());

                _taskCompletionSources.Add(taskCompletionSource);
                return taskCompletionSource.Task;
            }
        }

        public void ResumeReceiving()
        {
            lock (_messages)
            {
                _receivingPaused = false;
            }
        }

        private bool IsQueueEmpty()
        {
            lock (_messages)
            {
                var now = RebusTime.Now;
                return _messages.All(m => m.VisibleAfter > now + _options.DeferralProcessingLimit);
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
