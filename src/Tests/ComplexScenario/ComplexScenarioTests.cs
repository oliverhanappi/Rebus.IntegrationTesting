using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using NodaTime;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    [TestFixture]
    public class ComplexScenarioTests
    {
        private const string InputQueue = "input";
        private const string AuditQueue = "audit";
        private const string TestCorrelationId = "corr";

        private IContainer _container;
        private IIntegrationTestingBus _bus;

        [SetUp]
        public void SetUp()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(SystemClock.Instance).AsImplementedInterfaces();
            containerBuilder.RegisterInstance(new DocumentProcessingOptions(TimeSpan.FromSeconds(30)));
            containerBuilder.RegisterHandler<ProcessDocumentsCommandMonitoringHandler>();
            containerBuilder.RegisterHandler<DocumentProcessingSaga>();
            containerBuilder.RegisterHandler<AddWatermarkToDocumentCommandHandler>();
            containerBuilder.RegisterRebus(ConfigureRebus);

            _container = containerBuilder.Build();
            _bus = (IIntegrationTestingBus) _container.Resolve<IBus>();

            RebusConfigurer ConfigureRebus(RebusConfigurer configurer) => CommonRebusConfiguration
                .Apply(configurer, InputQueue, AuditQueue)
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
        public async Task ReturnsFailureReplyOnErrors()
        {
            var reply = await Execute<DocumentProcessingFailedReply>(AddWatermarkToDocumentCommandHandler.ErrorMarker);
            Assert.That(reply.CorrelationId, Is.EqualTo(TestCorrelationId));
            Assert.That(reply.ErrorDetails, Does.Contain(AddWatermarkToDocumentCommandHandler.ErrorMarker));
        }

        [Test]
        public async Task Success_PublishesMonitoringEvents()
        {
            var before = SystemClock.Instance.GetCurrentInstant();
            await Execute("Hello", "World");
            var after = SystemClock.Instance.GetCurrentInstant();

            var messages = _bus.PublishedMessages
                .OfType<DocumentProcessingMonitoringEvent>()
                .OrderBy(e => e.MessageTime)
                .ToList();

            Assert.That(messages, Has.Count.EqualTo(2));
            
            Assert.That(messages[0].Message, Is.EqualTo("Processing of 2 documents started."));
            Assert.That(messages[0].MessageTime, Is.InRange(before, after));
            
            Assert.That(messages[1].Message, Does.StartWith("Processing of 2 documents finished"));
            Assert.That(messages[1].MessageTime, Is.InRange(before, after));
        }

        [Test]
        public async Task Failure_PublishesMonitoringEvents()
        {
            await Execute(AddWatermarkToDocumentCommandHandler.ErrorMarker);

            var messages = _bus.PublishedMessages
                .OfType<DocumentProcessingMonitoringEvent>()
                .OrderBy(e => e.MessageTime)
                .ToList();

            Assert.That(messages, Has.Count.EqualTo(2));
            Assert.That(messages[0].Message, Is.EqualTo("Processing of 1 documents started."));
            Assert.That(messages[1].Message, Does.StartWith("Processing of 1 documents failed"));
        }

        [Test]
        public async Task RepliesFailureOnTimeout()
        {
            await Execute("Hello", AddWatermarkToDocumentCommandHandler.NoResponseMarker);

            Assert.That(_bus.RepliedMessages, Is.Empty);

            _bus.ShiftTime(TimeSpan.FromSeconds(30));
            await _bus.ProcessPendingMessages();

            var reply = (DocumentProcessingFailedReply) _bus.RepliedMessages.Single();
            Assert.That(reply.CorrelationId, Is.EqualTo(TestCorrelationId));
            Assert.That(reply.ErrorDetails, Does.Contain("timed out"));
        }

        [Test]
        public async Task AuditsMessages()
        {
            await Execute("Hello", "World");

            var auditMessages = _bus.GetMessages(AuditQueue);

            Assert.That(auditMessages, Has.Some.InstanceOf<ProcessDocumentsCommand>());
            Assert.That(auditMessages, Has.Some.InstanceOf<AddWatermarkToDocumentCommand>());
            Assert.That(auditMessages, Has.Some.InstanceOf<WatermarkAddedToDocumentReply>());
            Assert.That(auditMessages, Has.Some.InstanceOf<DocumentProcessingMonitoringEvent>());
        }

        private async Task<TExpectedReply> Execute<TExpectedReply>(params string[] contents)
        {
            await Execute(contents);

            Assert.That(_bus.RepliedMessages, Has.Count.EqualTo(1));
            Assert.That(_bus.RepliedMessages[0], Is.InstanceOf<TExpectedReply>());

            return (TExpectedReply) _bus.RepliedMessages[0];
        }

        private async Task Execute(params string[] contents)
        {
            var command = new ProcessDocumentsCommand(TestCorrelationId, contents.Select(CreateDocument).ToList());
            await _bus.ProcessMessage(command);
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
