using System;
using System.Collections.Generic;
using System.Reflection;
using Rebus.Persistence.InMem;
using Rebus.Sagas;

namespace Rebus.IntegrationTesting.Sagas
{
    public class IntegrationTestingSagaStorage : InMemorySagaStorage
    {
        private static readonly Lazy<PropertyInfo> InstancesProperty;

        static IntegrationTestingSagaStorage()
        {
            InstancesProperty = new Lazy<PropertyInfo>(GetInstancesProperty);

            PropertyInfo GetInstancesProperty()
            {
                var instancesProperty = typeof(InMemorySagaStorage).GetProperty("Instances",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                
                if (instancesProperty == null)
                    throw new InvalidOperationException("Unable to locate Instances property on InMemorySagaStorage.");
    
                return instancesProperty;
            }
        }


        public IEnumerable<ISagaData> SagaDatas => (IEnumerable<ISagaData>) InstancesProperty.Value.GetValue(this);
    }
}
