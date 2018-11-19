using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.IntegrationTesting
{
    public interface IMessages : IReadOnlyList<object>
    {
        IIntegrationTestingBus Bus { get; }
        
        IReadOnlyList<Message> GetMessages();
        IReadOnlyList<TransportMessage> GetTransportMessages();

        void Clear();
    }
}
