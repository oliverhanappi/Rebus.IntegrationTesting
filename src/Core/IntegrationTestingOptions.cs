using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Rebus.DataBus.InMem;
using Rebus.IntegrationTesting.Transport;
using Rebus.Persistence.InMem;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingOptions
    {
        [NotNull] public string InputQueueName { get; }
        [NotNull] public string SubscriberQueueName { get; }
        [NotNull] public string ReplyQueueName { get; }
        public TimeSpan DeferralProcessingLimit { get; }
        public int MaxProcessedMessages { get; }
        [NotNull] public JsonSerializerSettings SerializerSettings { get; }

        [NotNull] public IntegrationTestingNetwork Network { get; }
        [NotNull] public InMemDataStore DataStore { get; }
        [NotNull] public InMemorySubscriberStore SubscriberStore { get; }
        [NotNull] public InMemorySagaStorage SagaStorage { get; }

        public IntegrationTestingOptions([NotNull] string inputQueueName, [NotNull] string subscriberQueueName,
            [NotNull] string replyQueueName, TimeSpan deferralProcessingLimit, int maxProcessedMessages,
            [NotNull] JsonSerializerSettings serializerSettings)
        {
            InputQueueName = inputQueueName ?? throw new ArgumentNullException(nameof(inputQueueName));
            SubscriberQueueName = subscriberQueueName ?? throw new ArgumentNullException(nameof(subscriberQueueName));
            ReplyQueueName = replyQueueName ?? throw new ArgumentNullException(nameof(replyQueueName));
            DeferralProcessingLimit = deferralProcessingLimit;
            MaxProcessedMessages = maxProcessedMessages;
            SerializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
        }

        public IntegrationTestingOptions([NotNull] string inputQueueName, [NotNull] string subscriberQueueName,
            [NotNull] string replyQueueName, TimeSpan deferralProcessingLimit, int maxProcessedMessages,
            [NotNull] JsonSerializerSettings serializerSettings, [NotNull] IntegrationTestingNetwork network,
            [NotNull] InMemDataStore dataStore, [NotNull] InMemorySubscriberStore subscriberStore,
            [NotNull] InMemorySagaStorage sagaStorage)
        {
            InputQueueName = inputQueueName ?? throw new ArgumentNullException(nameof(inputQueueName));
            SubscriberQueueName = subscriberQueueName ?? throw new ArgumentNullException(nameof(subscriberQueueName));
            ReplyQueueName = replyQueueName ?? throw new ArgumentNullException(nameof(replyQueueName));
            DeferralProcessingLimit = deferralProcessingLimit;
            MaxProcessedMessages = maxProcessedMessages;
            SerializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
            Network = network ?? throw new ArgumentNullException(nameof(network));
            DataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            SubscriberStore = subscriberStore ?? throw new ArgumentNullException(nameof(subscriberStore));
            SagaStorage = sagaStorage ?? throw new ArgumentNullException(nameof(sagaStorage));
        }
    }
}
