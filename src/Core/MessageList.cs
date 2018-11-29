using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.IntegrationTesting
{
    public class MessageList : IMessages
    {
        private readonly ISerializer _serializer;
        private readonly List<TransportMessage> _transportMessages = new List<TransportMessage>();
        private readonly List<Message> _messages = new List<Message>();
        private readonly List<object> _messageBodies = new List<object>();

        public int Count => _messages.Count;
        public object this[int index] => _messageBodies[index];

        public IIntegrationTestingBus Bus { get; }

        public IReadOnlyList<IReadOnlyDictionary<string, string>> Headers =>
            _messages.Select(m => (IReadOnlyDictionary<string, string>) m.Headers).ToList();

        public MessageList([NotNull] ISerializer serializer, [NotNull] IIntegrationTestingBus bus)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task Add([NotNull] TransportMessage transportMessage)
        {
            if (transportMessage == null) throw new ArgumentNullException(nameof(transportMessage));

            var message = await _serializer.Deserialize(transportMessage);
            _transportMessages.Add(transportMessage);
            _messages.Add(message);
            _messageBodies.Add(message.Body);
        }

        public IEnumerator<object> GetEnumerator() => _messageBodies.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IReadOnlyList<Message> GetMessages() => _messages;
        public IReadOnlyList<TransportMessage> GetTransportMessages() => _transportMessages;

        public void Clear()
        {
            _transportMessages.Clear();
            _messages.Clear();
            _messageBodies.Clear();
        }
    }
}
