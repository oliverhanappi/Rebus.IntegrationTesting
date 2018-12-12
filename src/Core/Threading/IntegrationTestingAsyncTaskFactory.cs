using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Logging;
using Rebus.Threading;

namespace Rebus.IntegrationTesting.Threading
{
    public class IntegrationTestingAsyncTaskFactory : IAsyncTaskFactory
    {
        private readonly IRebusLoggerFactory _loggerFactory;
        private readonly ILog _log;

        private readonly ConcurrentBag<IntegrationTestingAsyncTask> _tasks =
            new ConcurrentBag<IntegrationTestingAsyncTask>();

        public IntegrationTestingAsyncTaskFactory([NotNull] IRebusLoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _log = loggerFactory.GetLogger<IntegrationTestingAsyncTaskFactory>();
        }
        
        public IAsyncTask Create([NotNull] string description, [NotNull] Func<Task> action, bool prettyInsignificant = false, int intervalSeconds = 10)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (intervalSeconds < 0) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            
            var task = new IntegrationTestingAsyncTask(description, action, intervalSeconds, _loggerFactory);
            
            _tasks.Add(task);
            
            _log.Info("Created task {0} with interval {1} seconds.", description, intervalSeconds);
            return task;
        }

        public async Task ExecuteDueTasks(CancellationToken cancellationToken = default)
        {
            foreach (var task in _tasks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (task.IsDue)
                {
                    await task.Execute();
                }
            }
        }
    }
}
