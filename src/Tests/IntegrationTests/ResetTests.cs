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
    public class ResetTests
    {
        private class Command
        {
            public string Value { get; set; }
        }

        private class Reply
        {
            public string Value { get; set; }
        }

        private class Event
        {
            public string Value { get; set; }
        }

        private class TestSagaData : SagaData
        {
            public string Value { get; set; }
        }
        
        [UsedImplicitly]
        private class TestSaga : Saga<TestSagaData>, IAmInitiatedBy<Command>
        {
            private readonly IBus _bus;

            public TestSaga(IBus bus)
            {
                _bus = bus;
            }

            protected override void CorrelateMessages(ICorrelationConfig<TestSagaData> config)
            {
                config.Correlate<Command>(c => c.Value, d => d.Value);
            }
            
            public async Task Handle(Command command)
            {
                Data.Value = command.Value;
                await _bus.Reply(new Reply {Value = command.Value.ToUpper()});
                await _bus.Publish(new Event {Value = command.Value.ToLower()});
                await _bus.Advanced.TransportMessage.Forward("forward");
            }
        }
        
        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterRebus(c => c.ConfigureForIntegrationTesting());
            containerBuilder.RegisterHandler<TestSaga>();
            _container = containerBuilder.Build();

            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Dispose();
        }
        
        [Test]
        public async Task ClearsAllPendingMessages()
        {
            await _bus.Send(new Command {Value = "Hello World"});
            
            Assert.That(_bus.PendingMessages, Has.Count.EqualTo(1));
            _bus.Reset();

            Assert.That(_bus.PendingMessages, Is.Empty);
        }
        
        [Test]
        public async Task ClearsAllProcessedMessages()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});
            
            Assert.That(_bus.ProcessedMessages, Has.Count.EqualTo(1));
            _bus.Reset();

            Assert.That(_bus.ProcessedMessages, Is.Empty);
        }
        
        [Test]
        public async Task ClearsAllRepliedMessages()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});
            
            Assert.That(_bus.RepliedMessages.Cast<Reply>().Single().Value, Is.EqualTo("HELLO WORLD"));
            _bus.Reset();

            Assert.That(_bus.RepliedMessages, Is.Empty);
        }
        
        [Test]
        public async Task ClearsAllPublishedMessages()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});
            
            Assert.That(_bus.PublishedMessages.Cast<Event>().Single().Value, Is.EqualTo("hello world"));
            _bus.Reset();

            Assert.That(_bus.PublishedMessages, Is.Empty);
        }
        
        [Test]
        public async Task ClearsAllMessagesInOtherQueues()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});
            var messages = _bus.GetMessages("forward");

            Assert.That(messages, Has.Count.EqualTo(1));
            _bus.Reset();

            Assert.That(messages, Is.Empty);
        }
        
        [Test]
        public void DeletesAllDataFromDataBus()
        {
            _bus.DataBusData.Save("1", new byte[] {1, 2, 3});

            Assert.That(_bus.DataBusData.AttachmentIds, Is.EquivalentTo(new[]{"1"}));
            Assert.That(_bus.DataBusData.SizeBytes, Is.EqualTo(3));
            
            _bus.Reset();
            
            Assert.That(_bus.DataBusData.AttachmentIds, Is.Empty);
            Assert.That(_bus.DataBusData.SizeBytes, Is.Zero);
        }

        [Test]
        public void DeletesAllSubscriptions()
        {
            _bus.Options.SubscriberStore.AddSubscriber("Hello", "World");

            Assert.That(_bus.Options.SubscriberStore.Topics, Does.Contain("Hello"));
            
            _bus.Reset();

            Assert.That(_bus.Options.SubscriberStore.Topics, Is.Empty);
        }

        [Test]
        public async Task DeletesAllSagaDataInstances()
        {
            await _bus.ProcessMessage(new Command {Value = "Hello World"});

            Assert.That(_bus.GetSagaDatas().Cast<TestSagaData>().Single().Value, Is.EqualTo("Hello World"));
            
            _bus.Reset();

            Assert.That(_bus.GetSagaDatas(), Is.Empty);
        }
    }
}
