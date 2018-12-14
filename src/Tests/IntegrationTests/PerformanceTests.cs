using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;

namespace Rebus.IntegrationTesting.Tests.IntegrationTests
{
    [TestFixture]
    public class PerformanceTests
    {
        private class Command
        {
            public int Value { get; set; }
        }

        [UsedImplicitly]
        private class CommandHandler : IHandleMessages<Command>
        {
            public Task Handle(Command command)
            {
                return Task.CompletedTask;
            }
        }

        [Test, Explicit("performance test")]
        public async Task PerformanceTest()
        {
            const int count = 1000;
            
            await PerformRun(0); // warm up

            var executionTimes = new List<TimeSpan>();
            for (var id = 1; id <= count; id++)
            {
                var stopwatch = Stopwatch.StartNew();
                await PerformRun(id);
                executionTimes.Add(stopwatch.Elapsed);
            }

            var minimum = executionTimes.Min(t => t.TotalMilliseconds);
            var maximum = executionTimes.Max(t => t.TotalMilliseconds);
            var average = executionTimes.Average(t => t.TotalMilliseconds);
            var median = executionTimes.OrderBy(t => t.Ticks).Select(t => t.TotalMilliseconds).ElementAt(count / 2);
            
            Console.WriteLine($"Minimum: {minimum:n0} ms");
            Console.WriteLine($"Average: {average:n0} ms");
            Console.WriteLine($"Median:  {median:n0} ms");
            Console.WriteLine($"Maximum: {maximum:n0} ms");
        }

        public async Task PerformRun(int id)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterHandler<CommandHandler>();
            containerBuilder.RegisterRebus(c => c
                .ConfigureForIntegrationTesting()
                .Logging(l => l.None()));
            
            using (var container = containerBuilder.Build())
            {
                var bus = (IIntegrationTestingBus) container.Resolve<IBus>();
                await bus.ProcessMessage(new Command {Value = id});

                var command = bus.ProcessedMessages.Cast<Command>().Single();
                Assert.That(command.Value, Is.EqualTo(id));
            }
        }
    }
}
