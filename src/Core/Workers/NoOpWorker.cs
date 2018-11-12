using System;
using JetBrains.Annotations;
using Rebus.Workers;

namespace Rebus.IntegrationTesting.Workers
{
    public class NoOpWorker : IWorker
    {
        [NotNull] public string Name { get; }

        public NoOpWorker([NotNull] string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}