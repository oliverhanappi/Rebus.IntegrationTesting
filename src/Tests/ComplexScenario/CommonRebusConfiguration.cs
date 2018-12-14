using System.Text;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using Rebus.Auditing.Messages;
using Rebus.Config;
using Rebus.Injection;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public static class CommonRebusConfiguration
    {
        public static RebusConfigurer Apply(RebusConfigurer configurer, string inputQueue, string auditQueue)
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            jsonSerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            return configurer
                    .Logging(l => l.Console())
                    .Serialization(s => s.UseNewtonsoftJson(jsonSerializerSettings, Encoding.UTF8))
                    .Routing(r => r.TypeBased().Map<AddWatermarkToDocumentCommand>(inputQueue))
                    .Options(o => o.SimpleRetryStrategy(secondLevelRetriesEnabled: true))
                    .Options(o => o.EnableMessageAuditing(auditQueue))
                    .Options(o => o.Decorate(DecoratePipeline))
                ;
        }

        private static IPipeline DecoratePipeline(IResolutionContext context)
        {
            var pipeline = context.Get<IPipeline>();
            return new PipelineStepInjector(pipeline)
                .OnSend(new RequestIdPipelineStep(), PipelineRelativePosition.Before, typeof(SendOutgoingMessageStep));
        }
    }
}
