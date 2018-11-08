using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Subscriptions;

namespace Rebus.IntegrationTesting.Subscriptions
{
    public class IntegrationTestingSubscriptionStorage : ISubscriptionStorage
    {
        private readonly IntegrationTestingOptions _integrationTestingOptions;

        public bool IsCentralized => true;

        public IntegrationTestingSubscriptionStorage([NotNull] IntegrationTestingOptions integrationTestingOptions)
        {
            _integrationTestingOptions = integrationTestingOptions
                                         ?? throw new ArgumentNullException(nameof(integrationTestingOptions));
        }
        
        public Task<string[]> GetSubscriberAddresses(string topic)
        {
            return Task.FromResult(new[] {_integrationTestingOptions.SubscriberQueueName});
        }

        public Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            return Task.CompletedTask;
        }

        public Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            return Task.CompletedTask;
        }
    }
}
