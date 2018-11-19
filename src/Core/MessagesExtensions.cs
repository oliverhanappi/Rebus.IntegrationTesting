using System;
using System.Text;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting
{
    public static class MessagesExtensions
    {
        public static string GetMessageSummary([NotNull] this IMessages messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            var summary = new StringBuilder();
            var serializerSettings = messages.Bus.Options.SerializerSettings;

            var messageList = messages.GetMessages();
            for (var i = 0; i < messageList.Count; i++)
            {
                var messageDetails = MessageDetails.FromMessage(messageList[i], serializerSettings);

                summary.AppendLine($"#{i + 1}:");
                summary.AppendLine(messageDetails.ToString());
                summary.AppendLine();
            }

            return summary.ToString().TrimEnd();
        }
    }
}
