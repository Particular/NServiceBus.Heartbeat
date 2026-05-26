namespace NServiceBus.Heartbeat
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;

    class HeartbeatsFeature : Feature
    {
        readonly ThroughputTracker throughputTracker = new();
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
            context.Pipeline.OnReceivePipelineCompleted((completed, ct) =>
            {
                throughputTracker.RecordMessage(completed);
                return Task.CompletedTask;
            });

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
                    ttl,
                    throughputTracker);
            });
        }
    }
}