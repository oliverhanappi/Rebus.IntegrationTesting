using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class DataBusTests
    {
        private class StoreValueCommand
        {
            public string Value { get; set; }
        }

        private class Reply
        {
            public string AttachmentId { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<StoreValueCommand>
        {
            private readonly IBus _bus;

            public CommandHandler(IBus bus)
            {
                _bus = bus;
            }
            
            public async Task Handle(StoreValueCommand command)
            {
                await Task.Yield();

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(command.Value)))
                {
                    var metadata = new Dictionary<string, string>{{nameof(StoreValueCommand.Value), command.Value}};
                    var attachment = await _bus.Advanced.DataBus.CreateAttachment(stream, metadata);
                    await _bus.Reply(new Reply {AttachmentId = attachment.Id});
                }
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
        public async Task StoresDataInDataBus()
        {
            await _bus.Send(new StoreValueCommand {Value = "Hello World"});

            var command = (StoreValueCommand) _bus.GetPendingMessages().Select(m => m.Body).Single();
            Assert.That(command.Value, Is.EqualTo("Hello World"));
            
            await _bus.ProcessPendingMessages();

            var reply = (Reply) _bus.GetRepliedMessages().Select(m => m.Body).Single();

            var data = _bus.DataBusData.Load(reply.AttachmentId);
            Assert.That(Encoding.UTF8.GetString(data), Is.EqualTo("Hello World"));

            var metadata = _bus.DataBusData.LoadMetadata(reply.AttachmentId);
            Assert.That(metadata[nameof(StoreValueCommand.Value)], Is.EqualTo("Hello World"));
        }
    }
}
