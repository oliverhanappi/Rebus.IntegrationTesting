using System;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Injection;
using Rebus.IntegrationTesting.Subscriptions;
using Rebus.IntegrationTesting.Transport;
using Rebus.Routing;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.IntegrationTesting
{
    public static class RebusConfigurerExtensions
    {
        public static RebusConfigurer ConfigureForIntegrationTesting([NotNull] this RebusConfigurer rebusConfigurer,
            [CanBeNull] IntegrationTestingOptions integrationTestingOptions = null)
        {
            if (rebusConfigurer == null) throw new ArgumentNullException(nameof(rebusConfigurer));

            integrationTestingOptions = integrationTestingOptions ?? new IntegrationTestingOptions();

            return rebusConfigurer
                .Options(o => o.Register(_ => integrationTestingOptions))
                .Options(o => o.Register(_ => new IntegrationTestingNetwork()))
                .Options(o => o.SetNumberOfWorkers(0))
                .Options(o => o.Decorate(CreateBusDecorator))
                .Transport(t => t.Register(CreateTransport))
                .Subscriptions(s => s.Register(CreateSubscriptionStorage));
        }

        private static ITransport CreateTransport(IResolutionContext resolutionContext)
        {
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            var network = resolutionContext.Get<IntegrationTestingNetwork>();
            return new IntegrationTestingTransport(network, options.InputQueueName);
        }

        private static ISubscriptionStorage CreateSubscriptionStorage(IResolutionContext resolutionContext)
        {
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            return new IntegrationTestingSubscriptionStorage(options);
        }

        private static IBus CreateBusDecorator(IResolutionContext resolutionContext)
        {
            var bus = resolutionContext.Get<IBus>();
            var network = resolutionContext.Get<IntegrationTestingNetwork>();
            var serializer = resolutionContext.Get<ISerializer>();
            var router = resolutionContext.Get<IRouter>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();

            return new IntegrationTestingBusDecorator(bus, network, serializer, router, options);
        }
    }
}
