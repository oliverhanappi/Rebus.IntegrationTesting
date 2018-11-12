using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting
{
    public static class IntegrationTestingBusExtensions
    {
        public static void DecreaseDeferral([NotNull] this IIntegrationTestingBus bus, int milliseconds)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            bus.DecreaseDeferral(TimeSpan.FromMilliseconds(milliseconds));
        }

        public static Task ProcessMessage([NotNull] this IIntegrationTestingBus bus,
            [NotNull] object message, CancellationToken cancellationToken = default)
        {
            return bus.ProcessMessage(message, null, cancellationToken);
        }

        public static async Task ProcessMessage([NotNull] this IIntegrationTestingBus bus,
            [NotNull] object message, [CanBeNull] Dictionary<string, string> optionalHeaders,
            CancellationToken cancellationToken = default)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (bus.GetPendingMessages().Count > 0)
                throw new InvalidOperationException("There are already some messages pending.");
            
            await bus.SendLocal(message);
            await bus.ProcessPendingMessages(cancellationToken);
        }
    }
}
