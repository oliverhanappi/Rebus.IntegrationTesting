using System;
using JetBrains.Annotations;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Utils
{
    public static class TransactionContextExtensions
    {
        [NotNull]
        public static T GetOrAdd<T>([NotNull] this ITransactionContext transactionContext, [NotNull] Func<T> factory)
        {
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return transactionContext.GetOrAdd(GetTypeKey<T>(), factory);
        }

        private static string GetTypeKey<T>()
        {
            return $"rb-it-{typeof(T).FullName}";
        }
    }
}
