namespace NServiceBus.Heartbeat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Hosting;
    using Logging;
    using ServiceControl.Plugin;
    using ServiceControl.Plugin.Heartbeat.Messages;
    using ServiceControl.Plugin.Nsb6.Heartbeat;
    using Transport;

    class HeartbeatSender : FeatureStartupTask, IDisposable
    {
        public HeartbeatSender(IDispatchMessages dispatcher, HostInformation hostInfo, ServiceControlBackend backend,
            string endpointName, TimeSpan interval, TimeSpan timeToLive)
        {
            this.dispatcher = dispatcher;
            this.backend = backend;
            this.endpointName = endpointName;
            heartbeatInterval = interval;
            ttlTimeSpan = timeToLive;
            this.hostInfo = hostInfo;
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
                var message = new RegisterEndpointStartup
                {
                    HostId = hostInfo.HostId,
                    Host = hostInfo.DisplayName,
                    Endpoint = endpointName,
                    HostDisplayName = hostInfo.DisplayName,
                    HostProperties = hostInfo.Properties,
                    StartedAt = startupTime
                };
                await backend.Send(message, ttlTimeSpan, dispatcher).ConfigureAwait(false);
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
            var message = new EndpointHeartbeat
            {
                ExecutedAt = DateTime.UtcNow,
                EndpointName = endpointName,
                Host = hostInfo.DisplayName,
                HostId = hostInfo.HostId
            };

            try
            {
                await backend.Send(message, ttlTimeSpan, dispatcher).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn("Unable to send heartbeat to ServiceControl.", ex);
            }
        }

        bool resendRegistration = true;
        ServiceControlBackend backend;
        IDispatchMessages dispatcher;
        CancellationTokenSource cancellationTokenSource;
        string endpointName;
        AsyncTimer heartbeatTimer;
        TimeSpan ttlTimeSpan;
        TimeSpan heartbeatInterval;
        TimeSpan registrationRetryInterval = TimeSpan.FromMinutes(1);
        HostInformation hostInfo;

        static ILog Logger = LogManager.GetLogger(typeof(HeartbeatSender));
    }
}