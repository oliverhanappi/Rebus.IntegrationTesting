using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.Variance;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class ContravarianceTests
    {
        private abstract class BaseMessage
        {
        }

        private class DerivedMessage : BaseMessage
        {
        }

        private class Reply
        {
            public string ErrorDetails { get; set; }
        }

        private class TestHandler : IHandleMessages<BaseMessage>, IHandleMessages<IFailed<BaseMessage>>
        {
            private readonly IBus _bus;

            [UsedImplicitly]
            public TestHandler(IBus bus)
            {
                _bus = bus;
            }

            public Task Handle(BaseMessage message) =>
                throw new Exception("oops");

            public Task Handle(IFailed<BaseMessage> failure) =>
                _bus.Reply(new Reply {ErrorDetails = failure.ErrorDescription});
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;
        
        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterSource(new ContravariantRegistrationSource());
            containerBuilder.RegisterHandler<TestHandler>();
            containerBuilder.RegisterRebus(c => c
                .Options(o => o.SimpleRetryStrategy(secondLevelRetriesEnabled: true))
                .ConfigureForIntegrationTesting());

            _container = containerBuilder.Build();
            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
        
        [Test]
        public async Task HandlesSecondLevelFailuresForDerivedMessages()
        {
            await _bus.ProcessMessage(new DerivedMessage());

            var reply = (Reply) _bus.RepliedMessages.Single();
            Assert.That(reply.ErrorDetails, Does.Contain("oops"));
        }
    }
}
