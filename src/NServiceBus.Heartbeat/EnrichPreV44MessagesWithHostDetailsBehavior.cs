namespace ServiceControl.Plugin.Nsb6.Heartbeat
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    class EnrichPreV44MessagesWithHostDetailsBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public EnrichPreV44MessagesWithHostDetailsBehavior(ReadOnlySettings settings)
        {
            hostId = settings.Get<Guid>("NServiceBus.HostInformation.HostId");
            host = settings.Get<string>("NServiceBus.HostInformation.DisplayName");
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            if (!context.Headers.ContainsKey("$.diagnostics.hostid"))
            {
                context.Headers["$.diagnostics.hostid"] = hostId.ToString();
                context.Headers["$.diagnostics.hostdisplayname"] = host;
            }

            return next();
        }

        Guid hostId;
        string host;
    }
}