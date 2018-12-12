using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Logging;
using Rebus.Threading;
using Rebus.Time;

namespace Rebus.IntegrationTesting.Threading
{
    public class IntegrationTestingAsyncTask : IAsyncTask
    {
        private readonly ILog _log;
        
        private readonly string _description;
        private readonly Func<Task> _action;
        private readonly int _intervalSeconds;

        private DateTimeOffset? _nextRunTime;

        public bool IsDue => _nextRunTime != null && _nextRunTime.Value >= RebusTime.Now;

        public IntegrationTestingAsyncTask([NotNull] string description, [NotNull] Func<Task> action,
            int intervalSeconds, IRebusLoggerFactory loggerFactory)
        {
            if (intervalSeconds < 0) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _log = loggerFactory.GetLogger<IntegrationTestingAsyncTask>();
            _description = description ?? throw new ArgumentNullException(nameof(description));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _intervalSeconds = intervalSeconds;
        }
        
        public void Start()
        {
            _nextRunTime = RebusTime.Now;
        }

        public async Task Execute()
        {
            try
            {
                _log.Info("Executing task {0}...", _description);
                
                await _action.Invoke();
                
                _log.Info("Executed task {0}.", _description);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Task {0} failed.", _description);
            }
            finally
            {
                _nextRunTime = RebusTime.Now + TimeSpan.FromSeconds(_intervalSeconds);
            }
        }

        public void Dispose()
        {
            _nextRunTime = null;
        }
    }
}