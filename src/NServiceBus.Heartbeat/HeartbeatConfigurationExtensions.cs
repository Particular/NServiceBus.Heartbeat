﻿namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Heartbeat;

    /// <summary>
    /// Plugin extension methods.
    /// </summary>
    public static class HeartbeatConfigurationExtensions
    {
        /// <summary>
        /// Sets the ServiceControl queue address.
        /// </summary>
        /// <param name="config">The enddpoint congfiguration to modify.</param>
        /// <param name="serviceControlQueue">ServiceControl queue address.</param>
        /// <param name="frequency">The frequency to send heartbeats.</param>
        /// <param name="timeToLive">The maximum time to live for the heartbeat.</param>
        public static void SendHeartbeatTo(this EndpointConfiguration config, string serviceControlQueue, TimeSpan? frequency = null, TimeSpan? timeToLive = null)
        {
            config.EnableFeature<HeartbeatsFeature>();
            config.GetSettings().Set("NServiceBus.Heartbeat.Queue", serviceControlQueue);
            if (timeToLive.HasValue)
            {
                config.GetSettings().Set("NServiceBus.Heartbeat.Ttl", timeToLive.Value);
            }
            if (frequency.HasValue)
            {
                config.GetSettings().Set("NServiceBus.Heartbeat.Interval", frequency.Value);
            }
        }
    }
}