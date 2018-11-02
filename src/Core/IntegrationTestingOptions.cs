using System;
using JetBrains.Annotations;
using Rebus.IntegrationTesting.Transport;

namespace Rebus.IntegrationTesting
{
    public class IntegrationTestingOptions
    {
        private string _inputQueueName = "InputQueue";
        private string _subscriberQueueName = "SubscriberQueue";
        private string _replyQueueName = "ReplyQueue";
        private TimeSpan _deferralProcessingLimit = TimeSpan.FromSeconds(1);
        private TimeSpan _maxProcessingTime = TimeSpan.FromSeconds(60);
        private int _numberOfWorkers = 1;

        [NotNull]
        public string InputQueueName
        {
            get => _inputQueueName;
            set => _inputQueueName = value ?? throw new ArgumentNullException(nameof(value));
        }

        [NotNull]
        public string SubscriberQueueName
        {
            get => _subscriberQueueName;
            set => _subscriberQueueName = value ?? throw new ArgumentNullException(nameof(value));
        }

        [NotNull]
        public string ReplyQueueName
        {
            get => _replyQueueName;
            set => _replyQueueName = value ?? throw new ArgumentNullException(nameof(value));
        }

        public TimeSpan DeferralProcessingLimit
        {
            get => _deferralProcessingLimit;
            set
            {
                if (value.Ticks < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Deferral processing limit must not be negative, but was {value}");
                }

                _deferralProcessingLimit = value;
            }
        }

        public TimeSpan MaxProcessingTime
        {
            get => _maxProcessingTime;
            set
            {
                if (value.Ticks <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Max processing time must not be positive, but was {value}");
                }

                _maxProcessingTime = value;
            }
        }

        public int NumberOfWorkers
        {
            get => _numberOfWorkers;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Number of workers must be greater then zero, but was {value}.");
                }

                _numberOfWorkers = value;
            }
        }
    }
}
