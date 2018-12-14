using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class RequestIdPipelineStep : IOutgoingStep
    {
        public const string RequestIdHeaderName = "X-Request-Id";
        
        public Task Process(OutgoingStepContext context, Func<Task> next)
        {
            if (MessageContext.Current != null)
            {
                if (MessageContext.Current.Headers.TryGetValue(RequestIdHeaderName, out var requestId))
                {
                    var message = context.Load<TransportMessage>();
                    message.Headers[RequestIdHeaderName] = requestId;
                }
            }

            return next();
        }
    }
}
