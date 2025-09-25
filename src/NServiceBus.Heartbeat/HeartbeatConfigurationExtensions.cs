namespace NServiceBus
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
        /// <param name="config">The endpoint configuration to modify.</param>
        /// <param name="serviceControlQueue">ServiceControl queue address.</param>
        /// <param name="frequency">The frequency to send heartbeats.</param>
        /// <param name="timeToLive">The maximum time to live for the heartbeat.</param>
        public static void SendHeartbeatTo(this EndpointConfiguration config, string serviceControlQueue, TimeSpan? frequency = null, TimeSpan? timeToLive = null)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceControlQueue);

            var settings = config.GetSettings();

            settings.Set("NServiceBus.Heartbeat.Queue", serviceControlQueue);

            if (frequency.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(frequency.Value, TimeSpan.Zero);
                settings.Set("NServiceBus.Heartbeat.Interval", frequency.Value);
            }

            if (timeToLive.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(timeToLive.Value, TimeSpan.Zero);
                settings.Set("NServiceBus.Heartbeat.Ttl", timeToLive.Value);
            }

            settings.AddStartupDiagnosticsSection("Manifest-HearbeatsQueue", serviceControlQueue);

            config.EnableFeature<HeartbeatsFeature>();
        }
    }
}