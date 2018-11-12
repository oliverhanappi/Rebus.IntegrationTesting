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

            var message = _bus.GetPublishedMessages().Single();
            Assert.That(message.Body, Is.InstanceOf<DocumentProcessingExecutedEvent>());
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
            
            Assert.That(_bus.GetRepliedMessages(), Is.Empty);
            
            _bus.DecreaseDeferral(TimeSpan.FromSeconds(30));
            await _bus.ProcessPendingMessages();

            var reply = (DocumentProcessingFailedReply) _bus.GetRepliedMessages().Single().Body;
            Assert.That(reply.CorrelationId, Is.EqualTo(TestCorrelationId));
            Assert.That(reply.ErrorDetails, Does.Contain("timed out"));
        }

        private async Task<TExpectedReply> Execute<TExpectedReply>(params string[] contents)
        {
            var command = new ProcessDocumentsCommand(TestCorrelationId, contents.Select(CreateDocument).ToList());
            await _bus.ProcessMessage(command);

            var repliedMessages = _bus.GetRepliedMessages();

            Assert.That(repliedMessages, Has.Count.EqualTo(1));
            Assert.That(repliedMessages[0].Body, Is.InstanceOf<TExpectedReply>());

            return (TExpectedReply) repliedMessages[0].Body;
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
