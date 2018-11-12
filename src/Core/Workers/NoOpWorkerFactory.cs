using Rebus.Workers;

namespace Rebus.IntegrationTesting.Workers
{
    public class NoOpWorkerFactory : IWorkerFactory
    {
        public IWorker CreateWorker(string workerName)
        {
            return new NoOpWorker(workerName);
        }
    }
}
