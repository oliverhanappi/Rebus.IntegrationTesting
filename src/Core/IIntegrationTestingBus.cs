using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;

namespace Rebus.IntegrationTesting
{
    public interface IIntegrationTestingBus : IBus
    {
        Task ProcessPendingMessages(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Message>> GetPendingMessages();
        Task<IReadOnlyList<Message>> GetPublishedMessages();
        Task<IReadOnlyList<Message>> GetRepliedMessages();
        Task<IReadOnlyList<Message>> GetMessages(string queueName);
    }
}
