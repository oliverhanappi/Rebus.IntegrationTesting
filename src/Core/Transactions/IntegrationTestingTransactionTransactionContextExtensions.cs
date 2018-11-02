using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Utils;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Transactions
{
    public static class IntegrationTestingTransactionTransactionContextExtensions
    {
        [NotNull]
        public static IntegrationTestingTransaction GetTransaction([NotNull] this ITransactionContext transactionContext)
        {
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            return transactionContext.GetOrAdd(CreateTransaction);

            IntegrationTestingTransaction CreateTransaction()
            {
                var transaction = new IntegrationTestingTransaction();

                transactionContext.OnCommitted(() =>
                {
                    transaction.Commit();
                    return Task.CompletedTask;
                });
                
                transactionContext.OnDisposed(() => transaction.Dispose());

                return transaction;
            }
        }
    }
}