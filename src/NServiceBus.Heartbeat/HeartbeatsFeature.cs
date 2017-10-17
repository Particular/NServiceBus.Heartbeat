namespace NServiceBus.Heartbeat
{
    using System;
    using Features;
    using Hosting;
    using ServiceControl.Plugin;
    using Transport;

    class HeartbeatsFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet("NServiceBus.Heartbeat.Interval", out TimeSpan interval))
            {
                interval = TimeSpan.FromSeconds(10);
            }
            if (!context.Settings.TryGet("NServiceBus.Heartbeat.Ttl", out TimeSpan ttl))
            {
                ttl = TimeSpan.FromTicks(interval.Ticks * 4);
            }
            var destinationQueue = context.Settings.Get<string>("NServiceBus.Heartbeat.Queue");
            var backend = new ServiceControlBackend(destinationQueue, context.Settings.LocalAddress());

            context.RegisterStartupTask(b => new HeartbeatSender(b.Build<IDispatchMessages>(), b.Build<HostInformation>(),
                backend, context.Settings.EndpointName(), interval, ttl));
        }
    }
}