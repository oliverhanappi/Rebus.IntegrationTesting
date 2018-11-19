using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Sagas;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class SagaTests
    {
        private class Command
        {
            public string Value { get; set; }
        }

        private class TestSagaData : SagaData
        {
            public string Value { get; set; }
            public int Count { get; set; }
        }
        
        [UsedImplicitly]
        private class CommandHandler : Saga<TestSagaData>, IAmInitiatedBy<Command>
        {
            public async Task Handle(Command command)
            {
                await Task.Yield();

                Data.Value = command.Value;
                Data.Count++;
            }

            protected override void CorrelateMessages(ICorrelationConfig<TestSagaData> config)
            {
                config.Correlate<Command>(c => c.Value, d => d.Value);
            }
        }

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c.ConfigureForIntegrationTesting());
            containerBuilder.RegisterHandler<CommandHandler>();
            _container = containerBuilder.Build();

            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }

        [Test]
        public async Task CreatesSaga()
        {
            await _bus.SendLocal(new Command {Value = "Hello World"});
            Assert.That(() => _bus.GetSagaDatas(), Is.Empty);

            await _bus.ProcessPendingMessages();

            var sagaData1 = (TestSagaData) _bus.GetSagaDatas().Single();
            Assert.That(sagaData1.Value, Is.EqualTo("Hello World"));
            Assert.That(sagaData1.Count, Is.EqualTo(1));

            await _bus.SendLocal(new Command {Value = "Hello World"});
            await _bus.ProcessPendingMessages();

            var sagaData2 = (TestSagaData) _bus.GetSagaDatas().Single();
            Assert.That(sagaData2.Value, Is.EqualTo("Hello World"));
            Assert.That(sagaData2.Count, Is.EqualTo(2));
        }
    }
}
