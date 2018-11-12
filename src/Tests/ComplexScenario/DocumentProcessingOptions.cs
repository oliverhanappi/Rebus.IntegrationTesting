using System;

namespace Rebus.IntegrationTesting.Tests.ComplexScenario
{
    public class DocumentProcessingOptions
    {
        public TimeSpan WatermarkAddingTimeout { get; }

        public DocumentProcessingOptions(TimeSpan watermarkAddingTimeout)
        {
            if (watermarkAddingTimeout.Ticks <= 0)
            {
                var msg = $"Watermark adding timeout must be greater than zero, but was {watermarkAddingTimeout}";
                throw new ArgumentException(msg, nameof(watermarkAddingTimeout));
            }

            WatermarkAddingTimeout = watermarkAddingTimeout;
        }
    }
}