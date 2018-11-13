using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    [TestFixture]
    public class ComplexScenarioTests
    {
        private const string InputQueue = "input";
        private const string TestCorrelationId = "corr";

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(new DocumentProcessingOptions(TimeSpan.FromSeconds(30)));
            containerBuilder.RegisterHandler<DocumentProcessingSaga>();
            containerBuilder.RegisterHandler<AddWatermarkToDocumentCommandHandler>();
            containerBuilder.RegisterRebus(ConfigureRebus);

            _container = containerBuilder.Build();
            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();

            RebusConfigurer ConfigureRebus(RebusConfigurer configurer) => configurer
                .Routing(r => r.TypeBased().Map<AddWatermarkToDocumentCommand>(InputQueue))
                .Options(o => o.SimpleRetryStrategy(secondLevelRetriesEnabled: true))
                .ConfigureForIntegrationTesting(i => i.InputQueueName(InputQueue).DeferralProcessingLimit(1_000));
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }

        [Test]
        public async Task AddsWatermarkToDocumentsAndMergesThem()
        {
            var reply = await Execute<DocumentsProcessedReply>("Hello", "World");
            Assert.That(reply.CorrelationId, Is.EqualTo(TestCorrelationId));
            AssertDocument(reply.ResultAttachmentId, "HelloWATERMARKWorldWATERMARK");
        }

        [Test]
        public async Task PublishesExecutedEvent()
        {
            await Execute<DocumentsProcessedReply>("Hello", "World");

            var message = _bus.PublishedMessages.Single();
            Assert.That(message, Is.InstanceOf<DocumentProcessingExecutedEvent>());
        }

        [Test]
        public async Task RepliesFailureOnTimeout()
        {
            var command = new ProcessDocumentsCommand(TestCorrelationId, new[]
            {
                CreateDocument("Hello"),
                CreateDocument(AddWatermarkToDocumentCommandHandler.NoResponseMarker)
            });

            await _bus.ProcessMessage(command);
            
            Assert.That(_bus.RepliedMessages, Is.Empty);
            
            _bus.DecreaseDeferral(TimeSpan.FromSeconds(30));
            await _bus.ProcessPendingMessages();

            var reply = (DocumentProcessingFailedReply) _bus.RepliedMessages.Single();
            Assert.That(reply.CorrelationId, Is.EqualTo(TestCorrelationId));
            Assert.That(reply.ErrorDetails, Does.Contain("timed out"));
        }

        private async Task<TExpectedReply> Execute<TExpectedReply>(params string[] contents)
        {
            var command = new ProcessDocumentsCommand(TestCorrelationId, contents.Select(CreateDocument).ToList());
            await _bus.ProcessMessage(command);

            Assert.That(_bus.RepliedMessages, Has.Count.EqualTo(1));
            Assert.That(_bus.RepliedMessages[0], Is.InstanceOf<TExpectedReply>());

            return (TExpectedReply) _bus.RepliedMessages[0];
        }

        private string CreateDocument(string content)
        {
            var attachmentId = Guid.NewGuid().ToString();
            _bus.DataBusData.Save(attachmentId, Encoding.UTF8.GetBytes(content),
                metadata: new Dictionary<string, string>());

            return attachmentId;
        }

        private void AssertDocument(string id, string expectedContent)
        {
            var content = Encoding.UTF8.GetString(_bus.DataBusData.Load(id));
            Assert.That(content, Is.EqualTo(expectedContent));
        }
    }
}
