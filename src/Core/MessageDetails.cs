using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Rebus.Messages;

namespace Rebus.IntegrationTesting
{
    public class MessageDetails
    {
        public static MessageDetails FromMessage(Message message, JsonSerializerSettings serializerSettings = null)
        {
            serializerSettings = serializerSettings
                                 ?? new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto};
            
            var serializedBody = JsonConvert.SerializeObject(message.Body, Formatting.Indented, serializerSettings);

            var headers = new StringBuilder();
            foreach (var header in message.Headers.OrderBy(p => p.Key))
                headers.AppendLine($"{header.Key}: {header.Value}");

            return new MessageDetails(message, headers.ToString().TrimEnd(), serializedBody);
        }

        [NotNull] public Message Message { get; }

        [NotNull] public string Name => Message.Body?.GetType().FullName ?? "<unknown>";
        [NotNull] public string Headers { get; }
        [NotNull] public string Body { get; }

        public MessageDetails([NotNull] Message message, [NotNull] string headers, [NotNull] string body)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(Name);
            sb.AppendLine();
            sb.AppendLine("Headers:");
            sb.AppendLine(Headers);
            sb.AppendLine();
            sb.AppendLine("Body:");
            sb.AppendLine(Body);

            return sb.ToString().TrimEnd();
        }
    }
}
