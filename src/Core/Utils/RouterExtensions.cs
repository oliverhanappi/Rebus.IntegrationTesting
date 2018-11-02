using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Messages;
using Rebus.Routing;

namespace Rebus.IntegrationTesting.Utils
{
    public static class RouterExtensions
    {
        public static async Task<bool> IsMapped([NotNull] this IRouter router, [NotNull] Message message)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            if (message == null) throw new ArgumentNullException(nameof(message));

            try
            {
                var destinationAddress = await router.GetDestinationAddress(message);
                return !String.IsNullOrWhiteSpace(destinationAddress);
            }
            catch
            {
                return false;
            }
        }
    }
}
