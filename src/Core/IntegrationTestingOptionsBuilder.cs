using System;
using JetBrains.Annotations;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingOptionsBuilder
    {
        public const string DefaultInputQueueName = "InputQueue";
        public const string DefaultSubscriberQueueName = "SubscriberQueue";
        public const string DefaultReplyQueueName = "ReplyQueue";
        public static readonly TimeSpan DefaultDeferralProcessingLimit = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultMaxProcessingTime = TimeSpan.FromSeconds(60);
        public const int DefaultNumberOfWorkers = 1;

        private string _inputQueueName = DefaultInputQueueName;
        private string _subscriberQueueName = DefaultSubscriberQueueName;
        private string _replyQueueName = DefaultReplyQueueName;
        private TimeSpan _deferralProcessingLimit = DefaultDeferralProcessingLimit;
        private TimeSpan _maxProcessingTime = DefaultMaxProcessingTime;
        private bool _hasCustomSubscriptionStorage = false;

        public IntegrationTestingOptionsBuilder InputQueueName([NotNull] string inputQueueName)
        {
            _inputQueueName = inputQueueName ?? throw new ArgumentNullException(nameof(inputQueueName));
            return this;
        }

        public IntegrationTestingOptionsBuilder SubscriberQueueName([NotNull] string subscriberQueueName)
        {
            _subscriberQueueName = subscriberQueueName ?? throw new ArgumentNullException(nameof(subscriberQueueName));
            return this;
        }

        public IntegrationTestingOptionsBuilder ReplyQueueName([NotNull] string replyQueueName)
        {
            _replyQueueName = replyQueueName ?? throw new ArgumentNullException(nameof(replyQueueName));
            return this;
        }

        public IntegrationTestingOptionsBuilder DeferralProcessingLimit(double deferralProcessingLimitMilliseconds)
            => DeferralProcessingLimit(TimeSpan.FromMilliseconds(deferralProcessingLimitMilliseconds));
        
        public IntegrationTestingOptionsBuilder DeferralProcessingLimit(TimeSpan deferralProcessingLimit)
        {
            if (deferralProcessingLimit.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deferralProcessingLimit),
                    $"Deferral processing limit must not be negative, but was {deferralProcessingLimit}");
            }

            _deferralProcessingLimit = deferralProcessingLimit;
            return this;
        }

        public IntegrationTestingOptionsBuilder MaxProcessingTime(TimeSpan maxProcessingTime)
        {
            if (maxProcessingTime.Ticks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxProcessingTime),
                    $"Max processing time must not be positive, but was {maxProcessingTime}");
            }

            _maxProcessingTime = maxProcessingTime;
            return this;
        }

        public IntegrationTestingOptionsBuilder HasCustomSubscriptionStorage()
        {
            _hasCustomSubscriptionStorage = true;
            return this;
        }

        public IntegrationTestingOptions Build()
        {
            if (String.Equals(_inputQueueName, _replyQueueName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Input and reply queue names must not be the same, but were both {_inputQueueName}.");
            }

            if (String.Equals(_inputQueueName, _subscriberQueueName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Input and subscriber queue names must not be the same, but were both {_inputQueueName}");
            }

            if (String.Equals(_replyQueueName, _subscriberQueueName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Reply and subscriber queu enames must not be the same, but were both {_replyQueueName}");
            }

            return new IntegrationTestingOptions(_inputQueueName, _subscriberQueueName, _replyQueueName,
                _deferralProcessingLimit, _maxProcessingTime, _hasCustomSubscriptionStorage);
        }
    }
}
