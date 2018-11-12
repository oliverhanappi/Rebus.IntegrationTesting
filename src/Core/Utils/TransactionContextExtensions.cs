using System;
using JetBrains.Annotations;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Utils
{
    public static class TransactionContextExtensions
    {
        public static bool IsMessageHandlingTransaction([NotNull] this ITransactionContext transactionContext)
        {
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));
            return transactionContext.Items.TryGetValue(StepContext.StepContextKey, out var value) &&
                   value is IncomingStepContext;
        }
    }
}
