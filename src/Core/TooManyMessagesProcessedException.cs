using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Rebus.Messages;

namespace Rebus.IntegrationTesting
{
    public class TooManyMessagesProcessedException : Exception
    {
        private static string CreateMessage([NotNull] IMessages processedMessages)
        {
            if (processedMessages == null) throw new ArgumentNullException(nameof(processedMessages));

            var message = new StringBuilder();
            message.AppendLine(
                $"Message processing reached the limit of processing {processedMessages.Count} messages.");
            message.AppendLine("The following messages have been processed:");
            message.AppendLine();
            message.AppendLine(processedMessages.GetMessageSummary());

            return message.ToString().TrimEnd();
        }

        public IReadOnlyList<Message> ProcessedMessages { get; }
        
        public TooManyMessagesProcessedException([NotNull] IMessages processedMessages)
            : base(CreateMessage(processedMessages))
        {
            ProcessedMessages = processedMessages.GetMessages();
        }
    }
}
