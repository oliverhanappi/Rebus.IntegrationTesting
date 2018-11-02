using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Subscriptions;

namespace Rebus.IntegrationTesting.Subscriptions
{
    public class IntegrationTestingSubscriptionStorage : ISubscriptionStorage
    {
        private readonly IntegrationTestingOptions _integrationTestingOptions;

        private readonly ConcurrentDictionary<string, ISet<string>> _subscriptions
            = new ConcurrentDictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);
        
        public bool IsCentralized => true;

        public IntegrationTestingSubscriptionStorage([NotNull] IntegrationTestingOptions integrationTestingOptions)
        {
            _integrationTestingOptions = integrationTestingOptions
                                         ?? throw new ArgumentNullException(nameof(integrationTestingOptions));
        }
        
        public Task<string[]> GetSubscriberAddresses([NotNull] string topic)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            var subscriptions = GetSubscriptions(topic).ToArray();
            return Task.FromResult(subscriptions);
        }

        public Task RegisterSubscriber([NotNull] string topic, [NotNull] string subscriberAddress)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (subscriberAddress == null) throw new ArgumentNullException(nameof(subscriberAddress));
            
            GetSubscriptions(topic).Add(subscriberAddress);
            return Task.CompletedTask;
        }

        public Task UnregisterSubscriber([NotNull] string topic, [NotNull] string subscriberAddress)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (subscriberAddress == null) throw new ArgumentNullException(nameof(subscriberAddress));

            if (!String.Equals(subscriberAddress, _integrationTestingOptions.SubscriberQueueName, StringComparison.OrdinalIgnoreCase))
                GetSubscriptions(topic).Remove(subscriberAddress);

            return Task.CompletedTask;
        }

        private ISet<string> GetSubscriptions(string topic)
        {
            return _subscriptions.GetOrAdd(topic, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {_integrationTestingOptions.SubscriberQueueName});
        }
    }
}
