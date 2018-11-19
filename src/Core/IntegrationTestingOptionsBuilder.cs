using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingOptionsBuilder
    {
        public const string DefaultInputQueueName = "InputQueue";
        public const string DefaultSubscriberQueueName = "SubscriberQueue";
        public const string DefaultReplyQueueName = "ReplyQueue";
        public static readonly TimeSpan DefaultDeferralProcessingLimit = TimeSpan.FromSeconds(1);
        public static readonly int DefaultMaxProcessedMessage = 100;

        public static JsonSerializerSettings DefaultSerializerSettings => new JsonSerializerSettings
            {TypeNameHandling = TypeNameHandling.Auto};

        private string _inputQueueName = DefaultInputQueueName;
        private string _subscriberQueueName = DefaultSubscriberQueueName;
        private string _replyQueueName = DefaultReplyQueueName;
        private TimeSpan _deferralProcessingLimit = DefaultDeferralProcessingLimit;
        private int _maxProcessedMessages = DefaultMaxProcessedMessage;
        private bool _hasCustomSubscriptionStorage;
        private JsonSerializerSettings _serializerSettings = DefaultSerializerSettings;

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

        public IntegrationTestingOptionsBuilder MaxProcessedMessages(int maxProcessedMessages)
        {
            if (maxProcessedMessages <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxProcessedMessages),
                    $"Max processed messages must be positive, but was {maxProcessedMessages}");
            }

            _maxProcessedMessages = maxProcessedMessages;
            return this;
        }

        public IntegrationTestingOptionsBuilder HasCustomSubscriptionStorage()
        {
            _hasCustomSubscriptionStorage = true;
            return this;
        }

        public IntegrationTestingOptionsBuilder JsonSerializerSettings([NotNull] JsonSerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
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
                _deferralProcessingLimit, _maxProcessedMessages, _hasCustomSubscriptionStorage, _serializerSettings);
        }
    }
}
