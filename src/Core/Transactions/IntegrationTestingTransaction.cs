using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting.Transactions
{
    public class IntegrationTestingTransaction : IDisposable
    {
        private static int _nextId = 1;

        private bool _committed;
        private bool _disposed;
        
        private readonly IList<Action> _commitActions = new List<Action>();
        private readonly IList<Action> _disposeActions = new List<Action>();

        public int Id { get; }

        public IntegrationTestingTransaction()
        {
            Id = Interlocked.Increment(ref _nextId);
        }

        public void OnCommit([NotNull] Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            if (_committed)
                throw new InvalidOperationException($"{this} has already been committed.");

            _commitActions.Add(action);
        }

        public void OnDispose([NotNull] Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            if (_disposed)
                throw new InvalidOperationException($"{this} has already been disposed.");

            _disposeActions.Add(action);
        }
        
        public void Commit()
        {
            if (_committed)
                throw new InvalidOperationException($"{this} has already been committed.");

            _committed = true;
            
            foreach (var commitAction in _commitActions) 
                commitAction.Invoke();
        }

        public void Dispose()
        {
            if (_disposed)
                throw new InvalidOperationException($"{this} has already been disposed.");

            _disposed = true;
            
            foreach (var disposeAction in _disposeActions) 
                disposeAction.Invoke();
        }

        public override string ToString()
        {
            return $"Transaction #{Id}";
        }
    }
}
