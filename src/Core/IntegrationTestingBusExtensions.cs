using System;
using System.Collections.Generic;
using System.Text;
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

            if (bus.PendingMessages.Count > 0)
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine("There are already some messages pending:");
                errorMessage.AppendLine();
                errorMessage.AppendLine(bus.PendingMessages.GetMessageSummary());

                throw new InvalidOperationException(errorMessage.ToString().TrimEnd());
            }
            
            await bus.SendLocal(message, optionalHeaders);
            await bus.ProcessPendingMessages(cancellationToken);
        }
    }
}
