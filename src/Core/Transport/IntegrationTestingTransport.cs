using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.IntegrationTesting.Transport
{
    public class IntegrationTestingTransport : ITransport
    {
        private readonly IntegrationTestingNetwork _network;

        [NotNull] public string Address { get; }

        public IntegrationTestingTransport([NotNull] IntegrationTestingNetwork network, [NotNull] string inputQueueName)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            Address = inputQueueName ?? throw new ArgumentNullException(nameof(inputQueueName));
        }

        public void CreateQueue(string address)
        {
        }

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            _network.Send(destinationAddress, message, context);
            return Task.CompletedTask;
        }

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            var transportMessage = _network.Receive(Address, context);
            return Task.FromResult(transportMessage);
        }
    }
}
