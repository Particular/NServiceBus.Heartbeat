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

            var replyToAddress = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly")
                ? context.Settings.LocalAddress()
                : null;

            var destinationQueue = context.Settings.Get<string>("NServiceBus.Heartbeat.Queue");
            var backend = new ServiceControlBackend(destinationQueue, replyToAddress);

            context.RegisterStartupTask(b => new HeartbeatSender(b.GetRequiredService<IDispatchMessages>(), b.GetRequiredService<HostInformation>(),
                backend, context.Settings.EndpointName(), interval, ttl));
        }
    }
}