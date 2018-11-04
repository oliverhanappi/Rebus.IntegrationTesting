using System;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Injection;
using Rebus.IntegrationTesting.Sagas;
using Rebus.IntegrationTesting.Subscriptions;
using Rebus.IntegrationTesting.Transport;
using Rebus.Logging;
using Rebus.Routing;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.IntegrationTesting
{
    public static class RebusConfigurerExtensions
    {
        public static RebusConfigurer ConfigureForIntegrationTesting([NotNull] this RebusConfigurer rebusConfigurer,
            [CanBeNull] Action<IntegrationTestingOptionsBuilder> configure = null)
        {
            if (rebusConfigurer == null) throw new ArgumentNullException(nameof(rebusConfigurer));

            var optionsBuilder = new IntegrationTestingOptionsBuilder();
            configure?.Invoke(optionsBuilder);
            
            var integrationTestingOptions = optionsBuilder.Build();

            var inMemDataStore = new InMemDataStore();

            return rebusConfigurer
                .Options(o =>
                {
                    o.Register(_ => integrationTestingOptions);
                    o.Register(_ => inMemDataStore);
                    o.Register(CreateNetwork);
                    o.SetNumberOfWorkers(0);
                    o.SetMaxParallelism(1);
                    o.Decorate(CreateBusDecorator);
                    o.EnableDataBus().StoreInMemory(inMemDataStore);
                })
                .Transport(t => t.Register(CreateTransport))
                .Subscriptions(s => s.Register(CreateSubscriptionStorage))
                .Sagas(s => s.Register(CreateSagaStorage));
        }

        private static IntegrationTestingNetwork CreateNetwork(IResolutionContext resolutionContext)
        {
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            return new IntegrationTestingNetwork(options);
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

        private static ISagaStorage CreateSagaStorage(IResolutionContext resolutionContext)
        {
            return new IntegrationTestingSagaStorage();
        }

        private static IBus CreateBusDecorator(IResolutionContext resolutionContext)
        {
            var bus = resolutionContext.Get<IBus>();
            var network = resolutionContext.Get<IntegrationTestingNetwork>();
            var serializer = resolutionContext.Get<ISerializer>();
            var router = resolutionContext.Get<IRouter>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            var log = resolutionContext.Get<IRebusLoggerFactory>().GetLogger<IntegrationTestingBusDecorator>();
            var sagaStorage = (IntegrationTestingSagaStorage) resolutionContext.Get<ISagaStorage>();
            var inMemDataStore = resolutionContext.Get<InMemDataStore>();

            return new IntegrationTestingBusDecorator(
                bus, network, serializer, router, log, sagaStorage, options, inMemDataStore);
        }
    }
}
