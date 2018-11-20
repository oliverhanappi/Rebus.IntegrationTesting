using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rebus.DataBus;
using Rebus.DataBus.InMem;

namespace Rebus.IntegrationTesting
{
    public static class InMemDataStoreExtensions
    {
        public static DataBusAttachment Save([NotNull] this InMemDataStore dataStore, [NotNull] byte[] content,
            Dictionary<string, string> metadata = null)
        {
            if (dataStore == null) throw new ArgumentNullException(nameof(dataStore));
            if (content == null) throw new ArgumentNullException(nameof(content));

            var attachmentId = Guid.NewGuid().ToString();

            dataStore.Save(attachmentId, content, metadata);
            return new DataBusAttachment(attachmentId);
        }
    }
}
