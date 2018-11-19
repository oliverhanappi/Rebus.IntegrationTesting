using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Transport;
using Rebus.IntegrationTesting.Utils;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.IntegrationTesting
{
    public class MessagesQueueAdapter : IMessages
    {
        private readonly IntegrationTestingQueue _queue;
        private readonly ISerializer _serializer;

        public int Count => _queue.GetMessages().Count;
        public object this[int index] => GetMessages().Select(m => m.Body).ElementAt(index);

        public IIntegrationTestingBus Bus { get; }

        public MessagesQueueAdapter([NotNull] IntegrationTestingQueue queue, [NotNull] ISerializer serializer,
            [NotNull] IIntegrationTestingBus bus)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<object> GetEnumerator() => GetMessages().Select(m => m.Body).GetEnumerator();

        public IReadOnlyList<Message> GetMessages()
        {
            return GetTransportMessages().Select(Deserialize).ToList();
            
            Message Deserialize(TransportMessage transportMessage)
                => AsyncUtility.RunSync(() => _serializer.Deserialize(transportMessage));
        }

        public IReadOnlyList<TransportMessage> GetTransportMessages()
        {
            return _queue.GetMessages();
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }
}
