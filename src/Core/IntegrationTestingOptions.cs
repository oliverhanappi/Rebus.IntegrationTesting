using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingOptions
    {
        [NotNull] public string InputQueueName { get; }
        [NotNull] public string SubscriberQueueName { get; }
        [NotNull] public string ReplyQueueName { get; }
        public TimeSpan DeferralProcessingLimit { get; }
        public int MaxProcessedMessages { get; }
        public bool HasCustomSubscriptionStorage { get; }
        [NotNull] public JsonSerializerSettings SerializerSettings { get; }

        public IntegrationTestingOptions([NotNull] string inputQueueName, [NotNull] string subscriberQueueName,
            [NotNull] string replyQueueName, TimeSpan deferralProcessingLimit, int maxProcessedMessages,
            bool hasCustomSubscriptionStorage, [NotNull] JsonSerializerSettings serializerSettings)
        {
            InputQueueName = inputQueueName ?? throw new ArgumentNullException(nameof(inputQueueName));
            SubscriberQueueName = subscriberQueueName ?? throw new ArgumentNullException(nameof(subscriberQueueName));
            ReplyQueueName = replyQueueName ?? throw new ArgumentNullException(nameof(replyQueueName));
            DeferralProcessingLimit = deferralProcessingLimit;
            MaxProcessedMessages = maxProcessedMessages;
            HasCustomSubscriptionStorage = hasCustomSubscriptionStorage;
            SerializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
        }
    }
}
