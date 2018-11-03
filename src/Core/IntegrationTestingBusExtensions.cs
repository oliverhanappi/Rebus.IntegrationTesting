using System;
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
    }
}