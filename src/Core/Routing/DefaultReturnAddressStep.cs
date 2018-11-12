using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Utils;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Routing
{
    public class DefaultReturnAddressStep : IOutgoingStep
    {
        private readonly string _returnAddress;

        public DefaultReturnAddressStep([NotNull] string returnAddress)
        {
            _returnAddress = returnAddress ?? throw new ArgumentNullException(nameof(returnAddress));
        }

        public Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();
            if (!transactionContext.IsMessageHandlingTransaction())
            {
                var message = context.Load<Message>();
                message.Headers.Add(Headers.ReturnAddress, _returnAddress);
            }

            return next();
        }
    }
}
