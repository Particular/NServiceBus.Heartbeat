namespace ServiceControl.Features
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using Plugin;
    using Plugin.Heartbeat.Messages;
    using Plugin.Nsb6.Heartbeat;

    /// <summary>
    /// The ServiceControl.Heartbeat plugin.
    /// </summary>
    public class Heartbeats : Feature
    {
        internal Heartbeats()
        {
            EnableByDefault();
        }

        /// <summary>Called when the features is activated.</summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!VersionChecker.CoreVersionIsAtLeast(4, 4))
            {
                context.Pipeline.Register("EnrichPreV44MessagesWithHostDetailsBehavior", new EnrichPreV44MessagesWithHostDetailsBehavior(context.Settings), "Enriches pre v4 messages with details about the host");
            }

            context.RegisterStartupTask(builder => new HeartbeatStartup(builder.Build<IDispatchMessages>(), context.Settings));
        }

        static ILog Logger = LogManager.GetLogger(typeof(Heartbeats));

        [SkipWeaving]
        class HeartbeatStartup : FeatureStartupTask, IDisposable
        {
            public HeartbeatStartup(IDispatchMessages messageDispatcher, ReadOnlySettings settings)
            {
                backend = new ServiceControlBackend(messageDispatcher, settings);
                endpointName = settings.EndpointName();
                HostId = settings.Get<Guid>("NServiceBus.HostInformation.HostId");
                Host = settings.Get<string>("NServiceBus.HostInformation.DisplayName");
                Properties = settings.Get<Dictionary<string, string>>("NServiceBus.HostInformation.Properties");

                var interval = ConfigurationManager.AppSettings["Heartbeat/Interval"];
                if (!string.IsNullOrEmpty(interval))
                {
                    heartbeatInterval = TimeSpan.Parse(interval);
                }
                else if (settings.HasSetting("ServiceControl.Heartbeat.Interval"))
                {
                    heartbeatInterval = settings.Get<TimeSpan>("ServiceControl.Heartbeat.Interval");
                }

                ttlTimeSpan = TimeSpan.FromTicks(heartbeatInterval.Ticks*4); // Default ttl

                var ttl = ConfigurationManager.AppSettings["Heartbeat/TTL"];
                if (!string.IsNullOrEmpty(ttl))
                {
                    ttlTimeSpan = TimeSpan.Parse(ttl);
                }
                else if (settings.HasSetting("ServiceControl.Heartbeat.Ttl"))
                {
                    ttlTimeSpan = settings.Get<TimeSpan>("ServiceControl.Heartbeat.Ttl");
                }
            }

            public void Dispose()
            {
                cancellationTokenSource?.Dispose();
            }

            protected override Task OnStart(IMessageSession session)
            {
                cancellationTokenSource = new CancellationTokenSource();

                NotifyEndpointStartup(DateTime.UtcNow);
                StartHeartbeats();

                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                heartbeatTimer?.Stop();

                cancellationTokenSource?.Cancel();

                return Task.FromResult(0);
            }

            void NotifyEndpointStartup(DateTime startupTime)
            {
                // don't block here since StartupTasks are executed synchronously.
                SendEndpointStartupMessage(startupTime, cancellationTokenSource.Token).Ignore();
            }

            void StartHeartbeats()
            {
                Logger.Debug($"Start sending heartbeats every {heartbeatInterval}");
                heartbeatTimer = new AsyncTimer();
                heartbeatTimer.Start(SendHeartbeatMessage, heartbeatInterval, e => { });
            }

            async Task SendEndpointStartupMessage(DateTime startupTime, CancellationToken cancellationToken)
            {
                try
                {
                    await backend.Send(
                        new RegisterEndpointStartup
                        {
                            HostId = HostId,
                            Host = Host,
                            Endpoint = endpointName,
                            HostDisplayName = Host,
                            HostProperties = Properties,
                            StartedAt = startupTime
                        }, ttlTimeSpan).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!resendRegistration)
                    {
                        Logger.Warn("Unable to register endpoint startup with ServiceControl.", ex);
                        return;
                    }

                    resendRegistration = false;

                    Logger.Warn($"Unable to register endpoint startup with ServiceControl. Going to reattempt registration after {registrationRetryInterval}.", ex);

                    await Task.Delay(registrationRetryInterval, cancellationToken).ConfigureAwait(false);
                    await SendEndpointStartupMessage(startupTime, cancellationToken).ConfigureAwait(false);
                }
            }

            async Task SendHeartbeatMessage()
            {
                var heartBeat = new EndpointHeartbeat
                {
                    ExecutedAt = DateTime.UtcNow,
                    EndpointName = endpointName,
                    Host = Host,
                    HostId = HostId
                };

                try
                {
                    await backend.Send(heartBeat, ttlTimeSpan).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Unable to send heartbeat to ServiceControl.", ex);
                }
            }

            bool resendRegistration = true;
            ServiceControlBackend backend;
            CancellationTokenSource cancellationTokenSource;
            string endpointName;
            AsyncTimer heartbeatTimer;
            TimeSpan ttlTimeSpan;
            TimeSpan heartbeatInterval = TimeSpan.FromSeconds(10);
            TimeSpan registrationRetryInterval = TimeSpan.FromMinutes(1);
            Guid HostId;
            string Host;
            Dictionary<string, string> Properties;
        }
    }
}