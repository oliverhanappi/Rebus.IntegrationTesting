using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.DataBus.InMem;
using Rebus.Messages;
using Rebus.Sagas;

namespace Rebus.IntegrationTesting
{
    public interface IIntegrationTestingBus : IBus
    {
        Task ProcessPendingMessages(CancellationToken cancellationToken = default);
        void DecreaseDeferral(TimeSpan timeSpan);

        IReadOnlyList<Message> GetPendingMessages();
        IReadOnlyList<Message> GetPublishedMessages();
        IReadOnlyList<Message> GetRepliedMessages();
        IReadOnlyList<Message> GetMessages(string queueName);

        IReadOnlyCollection<ISagaData> GetSagaDatas();
        
        InMemDataStore DataBusData { get; }
    }
}
