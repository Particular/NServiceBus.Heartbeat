namespace NServiceBus.Heartbeat
{
    using System;
    using Features;
    using Hosting;
    using Transport;
    using Microsoft.Extensions.DependencyInjection;

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

            context.RegisterStartupTask(b =>
            {
                // Uses GetService because ReceiveAddresses is not registered on send-only endpoint.
                var backend = new ServiceControlBackend(destinationQueue, b.GetService<ReceiveAddresses>());

                return new HeartbeatSender(
                    b.GetRequiredService<IMessageDispatcher>(),
                    b.GetRequiredService<HostInformation>(),
                    backend,
                    context.Settings.EndpointName(),
                    interval,
                    ttl);
            });
        }
    }
}