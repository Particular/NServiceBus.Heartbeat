namespace NServiceBus.Heartbeat
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using NServiceBus;

    class ThroughputTracker
    {
        readonly ConcurrentDictionary<DateTime, long> receivedMessages = [];

        internal void RecordMessage(ReceivePipelineCompleted completed) => _ = receivedMessages.AddOrUpdate(completed.CompletedAt.Date, 1, (_, existing) => existing + 1);

        internal Dictionary<DateTime, long> ReadThroughput()
        {
            if (receivedMessages.IsEmpty)
            {
                return [];
            }

            var results = new Dictionary<DateTime, long>();
            foreach (var date in receivedMessages.Keys)
            {
                if (receivedMessages.Remove(date, out long messageCount))
                {
                    results.Add(date, messageCount);
                }
            }

            return results;
        }
    }
}
