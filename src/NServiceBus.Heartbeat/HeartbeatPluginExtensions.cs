namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Plugin extension methods.
    /// </summary>
    public static class HeartbeatPluginExtensions
    {
        /// <summary>
        /// Sets the ServiceControl queue address.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceControlQueue">ServiceControl queue address.</param>
        /// <param name="frequency">The frequency to send heartbeats.</param>
        /// <param name="timeToLive">The maximum time to live for the heartbeat.</param>
        public static void HeartbeatPlugin(this EndpointConfiguration config, string serviceControlQueue, TimeSpan? frequency = null, TimeSpan? timeToLive = null)
        {
            config.GetSettings().Set("ServiceControl.Queue", serviceControlQueue);
            if (timeToLive.HasValue)
            {
                config.GetSettings().Set("ServiceControl.Heartbeat.Ttl", timeToLive.Value);
            }
            if (frequency.HasValue)
            {
                config.GetSettings().Set("ServiceControl.Heartbeat.Interval", frequency.Value);
            }
        }
    }
}