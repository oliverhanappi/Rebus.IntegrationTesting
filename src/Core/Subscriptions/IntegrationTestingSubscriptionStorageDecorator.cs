using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Subscriptions;

namespace Rebus.IntegrationTesting.Subscriptions
{
    public class IntegrationTestingSubscriptionStorageDecorator : ISubscriptionStorage
    {
        private readonly ISubscriptionStorage _inner;
        private readonly string _defaultSubscriber;

        public bool IsCentralized => _inner.IsCentralized;

        public IntegrationTestingSubscriptionStorageDecorator([NotNull] ISubscriptionStorage inner,
            [NotNull] string defaultSubscriber)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _defaultSubscriber = defaultSubscriber ?? throw new ArgumentNullException(nameof(defaultSubscriber));
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            var subscribers = await _inner.GetSubscriberAddresses(topic);

            if (!subscribers.Contains(_defaultSubscriber))
                subscribers = subscribers.Concat(new[] {_defaultSubscriber}).ToArray();

            return subscribers;
        }

        public Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            return _inner.RegisterSubscriber(topic, subscriberAddress);
        }

        public Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            return _inner.UnregisterSubscriber(topic, subscriberAddress);
        }
    }
}
