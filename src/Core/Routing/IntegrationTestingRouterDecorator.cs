using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Utils;
using Rebus.Messages;
using Rebus.Routing;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Routing
{
    public class IntegrationTestingRouterDecorator : IRouter
    {
        private readonly IRouter _inner;
        private readonly string _inputQueue;

        public IntegrationTestingRouterDecorator([NotNull] IRouter inner, [NotNull] string inputQueue)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _inputQueue = inputQueue ?? throw new ArgumentNullException(nameof(inputQueue));
        }

        public async Task<string> GetDestinationAddress(Message message)
        {
            if (!IsHandlingMessage())
                return _inputQueue;

            return await _inner.GetDestinationAddress(message);
        }

        public async Task<string> GetOwnerAddress(string topic)
        {
            if (!IsHandlingMessage())
                return _inputQueue;

            return await _inner.GetOwnerAddress(topic);
        }

        private static bool IsHandlingMessage()
        {
            return AmbientTransactionContext.Current?.IsMessageHandlingTransaction() ?? false;
        }
    }
}
