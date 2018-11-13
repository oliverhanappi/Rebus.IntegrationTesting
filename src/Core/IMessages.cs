using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.IntegrationTesting
{
    public interface IMessages : IReadOnlyList<object>
    {
        IReadOnlyList<Message> GetMessages();
        IReadOnlyList<TransportMessage> GetTransportMessages();
    }
}
