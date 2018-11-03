using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Utils
{
    public static class AsyncUtility
    {
        public static void RunSync([NotNull] Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            RunSync(async () =>
            {
                await action();
                return 0;
            });
        }

        public static T RunSync<T>([NotNull] Func<Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            var originalSynchronizationContext = SynchronizationContext.Current;
            try
            {
                var singleThreadSynchronizationContext = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(singleThreadSynchronizationContext);

                var task = func.Invoke();
                task.ContinueWith(_ => singleThreadSynchronizationContext.Complete(), TaskScheduler.Default);

                singleThreadSynchronizationContext.RunOnCurrentThread();

                return task.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalSynchronizationContext);
            }
        }

        private sealed class SingleThreadSynchronizationContext : SynchronizationContext
        {
            private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> _queue =
                new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

            public override void Post(SendOrPostCallback callback, object state)
            {
                _queue.Add(new KeyValuePair<SendOrPostCallback, object>(callback, state));
            }

            public void RunOnCurrentThread()
            {
                KeyValuePair<SendOrPostCallback, object> workItem;
                while (_queue.TryTake(out workItem, Timeout.Infinite))
                    workItem.Key(workItem.Value);
            }

            public void Complete()
            {
                _queue.CompleteAdding();
            }
        }
    }
}
