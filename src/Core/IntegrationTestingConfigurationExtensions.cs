using System;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Injection;
using Rebus.IntegrationTesting.Routing;
using Rebus.IntegrationTesting.Sagas;
using Rebus.IntegrationTesting.Subscriptions;
using Rebus.IntegrationTesting.Transport;
using Rebus.IntegrationTesting.Workers;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Routing;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Transport;
using Rebus.Workers;

namespace Rebus.IntegrationTesting
{
    public static class IntegrationTestingConfigurationExtensions
    {
        public static RebusConfigurer ConfigureForIntegrationTesting([NotNull] this RebusConfigurer rebusConfigurer,
            [CanBeNull] Action<IntegrationTestingOptionsBuilder> configure = null)
        {
            if (rebusConfigurer == null) throw new ArgumentNullException(nameof(rebusConfigurer));

            var optionsBuilder = new IntegrationTestingOptionsBuilder();
            configure?.Invoke(optionsBuilder);
            
            var integrationTestingOptions = optionsBuilder.Build();

            var inMemDataStore = new InMemDataStore();

            if (!integrationTestingOptions.HasCustomSubscriptionStorage)
                rebusConfigurer = rebusConfigurer.Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()));
            
            return rebusConfigurer
                .Options(o =>
                {
                    o.Register(_ => integrationTestingOptions);
                    o.Register(_ => inMemDataStore);
                    o.Register(CreateNetwork);
                    o.Register(CreateWorkerFactory);

                    o.Decorate(InjectPipelineSteps);
                    o.Decorate(CreateRouterDecorator);
                    o.Decorate(CreateBusDecorator);
                    
                    o.SetNumberOfWorkers(0);
                    o.SetMaxParallelism(1);
                    o.EnableDataBus().StoreInMemory(inMemDataStore);
                })
                .Transport(t => t.Register(CreateTransport))
                .Subscriptions(s => s.Decorate(CreateSubscriptionStorageDecorator))
                .Sagas(s => s.Register(CreateSagaStorage));
        }

        private static IWorkerFactory CreateWorkerFactory(IResolutionContext resolutionContext)
        {
            return new NoOpWorkerFactory();
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

        private static ISubscriptionStorage CreateSubscriptionStorageDecorator(IResolutionContext resolutionContext)
        {
            var subscriptionStorage = resolutionContext.Get<ISubscriptionStorage>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            return new IntegrationTestingSubscriptionStorageDecorator(subscriptionStorage, options.SubscriberQueueName);
        }

        private static ISagaStorage CreateSagaStorage(IResolutionContext resolutionContext)
        {
            return new IntegrationTestingSagaStorage();
        }

        private static IRouter CreateRouterDecorator(IResolutionContext resolutionContext)
        {
            var router = resolutionContext.Get<IRouter>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            
            return new IntegrationTestingRouterDecorator(router, options.InputQueueName);
        }

        private static IPipeline InjectPipelineSteps(IResolutionContext resolutionContext)
        {
            var pipeline = resolutionContext.Get<IPipeline>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();

            var defaultReturnAddressStep = new DefaultReturnAddressStep(options.ReplyQueueName);
            var injector = new PipelineStepInjector(pipeline)
                .OnSend(defaultReturnAddressStep, PipelineRelativePosition.Before, typeof(AssignDefaultHeadersStep));

            return new PipelineStepRemover(injector)
                .RemoveIncomingStep(s => s is HandleDeferredMessagesStep);
        }

        private static IBus CreateBusDecorator(IResolutionContext resolutionContext)
        {
            var bus = resolutionContext.Get<IBus>();
            var network = resolutionContext.Get<IntegrationTestingNetwork>();
            var serializer = resolutionContext.Get<ISerializer>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            var sagaStorage = (IntegrationTestingSagaStorage) resolutionContext.Get<ISagaStorage>();
            var inMemDataStore = resolutionContext.Get<InMemDataStore>();
            var pipelineInvoker = resolutionContext.Get<IPipelineInvoker>();

            return new IntegrationTestingBusDecorator(
                bus, network, serializer, sagaStorage, options, inMemDataStore, pipelineInvoker);
        }
    }
}
