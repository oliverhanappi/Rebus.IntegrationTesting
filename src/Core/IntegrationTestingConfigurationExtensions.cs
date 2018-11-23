using System;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Injection;
using Rebus.IntegrationTesting.Routing;
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
            
            var options = optionsBuilder.Build();

            return rebusConfigurer
                .Transport(t => t.Register(CreateTransport))
                .Subscriptions(s => s.StoreInMemory(options.SubscriberStore))
                .Subscriptions(s => s.Decorate(DecorateSubscriptionStorage))
                .Sagas(s => s.Register(CreateSagaStorage))
                .Options(o =>
                {
                    o.Register(_ => options);
                    o.Register(CreateWorkerFactory);

                    o.Decorate(InjectPipelineSteps);
                    o.Decorate(DecorateRouter);
                    o.Decorate(DecorateBus);
                    
                    o.SetNumberOfWorkers(0);
                    o.SetMaxParallelism(1);
                    o.EnableDataBus().StoreInMemory(options.DataStore);
                });
        }

        private static IWorkerFactory CreateWorkerFactory(IResolutionContext resolutionContext)
        {
            return new NoOpWorkerFactory();
        }

        private static ITransport CreateTransport(IResolutionContext resolutionContext)
        {
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            return new IntegrationTestingTransport(options.Network, options.InputQueueName);
        }

        private static ISubscriptionStorage DecorateSubscriptionStorage(IResolutionContext resolutionContext)
        {
            var subscriptionStorage = resolutionContext.Get<ISubscriptionStorage>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            return new IntegrationTestingSubscriptionStorageDecorator(subscriptionStorage, options.SubscriberQueueName);
        }

        private static ISagaStorage CreateSagaStorage(IResolutionContext resolutionContext)
        {
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            return options.SagaStorage;
        }

        private static IRouter DecorateRouter(IResolutionContext resolutionContext)
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

        private static IBus DecorateBus(IResolutionContext resolutionContext)
        {
            var bus = resolutionContext.Get<IBus>();
            var serializer = resolutionContext.Get<ISerializer>();
            var options = resolutionContext.Get<IntegrationTestingOptions>();
            var pipelineInvoker = resolutionContext.Get<IPipelineInvoker>();

            return new IntegrationTestingBusDecorator(bus, serializer, options, pipelineInvoker);
        }
    }
}
