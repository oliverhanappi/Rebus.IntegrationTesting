using System;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Transport;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingOptions
    {
        [NotNull] public string InputQueueName { get; }
        [NotNull] public string SubscriberQueueName { get; }
        [NotNull] public string ReplyQueueName { get; }
        public TimeSpan DeferralProcessingLimit { get; }
        public TimeSpan MaxProcessingTime { get; }

        public int NumberOfWorkers { get; }

        public IntegrationTestingOptions([NotNull] string inputQueueName, [NotNull] string subscriberQueueName,
            [NotNull] string replyQueueName, TimeSpan deferralProcessingLimit, TimeSpan maxProcessingTime,
            int numberOfWorkers)
        {
            InputQueueName = inputQueueName ?? throw new ArgumentNullException(nameof(inputQueueName));
            SubscriberQueueName = subscriberQueueName ?? throw new ArgumentNullException(nameof(subscriberQueueName));
            ReplyQueueName = replyQueueName ?? throw new ArgumentNullException(nameof(replyQueueName));
            DeferralProcessingLimit = deferralProcessingLimit;
            MaxProcessingTime = maxProcessingTime;
            NumberOfWorkers = numberOfWorkers;
        }
    }
}
