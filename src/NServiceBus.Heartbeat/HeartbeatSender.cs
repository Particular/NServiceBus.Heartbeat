namespace NServiceBus.Heartbeat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Hosting;
    using Logging;
    using ServiceControl.Plugin.Heartbeat.Messages;
    using Transport;

    class HeartbeatSender : FeatureStartupTask, IDisposable
    {
        public HeartbeatSender(IMessageDispatcher dispatcher, HostInformation hostInfo, ServiceControlBackend backend,
            string endpointName, TimeSpan interval, TimeSpan timeToLive)
        {
            this.dispatcher = dispatcher;
            this.backend = backend;
            this.endpointName = endpointName;
            heartbeatInterval = interval;
            ttlTimeSpan = timeToLive;
            this.hostInfo = hostInfo;
        }

        public void Dispose() =>
            stopSendingHeartbeatsTokenSource?.Dispose();

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            stopSendingHeartbeatsTokenSource = new CancellationTokenSource();

            // don't block here since StartupTasks are executed synchronously.
            _ = SendEndpointStartupMessage(DateTime.UtcNow, stopSendingHeartbeatsTokenSource.Token);

            Logger.Debug($"Start sending heartbeats every {heartbeatInterval}");

            _ = Task.Run(async () =>
                {
                    while (!stopSendingHeartbeatsTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(heartbeatInterval, stopSendingHeartbeatsTokenSource.Token).ConfigureAwait(false);

                            var message = new EndpointHeartbeat
                            {
                                ExecutedAt = DateTime.UtcNow,
                                EndpointName = endpointName,
                                Host = hostInfo.DisplayName,
                                HostId = hostInfo.HostId
                            };

                            await backend.Send(message, ttlTimeSpan, dispatcher, stopSendingHeartbeatsTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // no-op
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn("Unable to send heartbeat to ServiceControl.", ex);
                        }
                    }
                },
                CancellationToken.None);

            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            stopSendingHeartbeatsTokenSource?.Cancel();

            return Task.CompletedTask;
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
                await backend.Send(message, ttlTimeSpan, dispatcher, cancellationToken).ConfigureAwait(false);
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

        bool resendRegistration = true;
        ServiceControlBackend backend;
        IMessageDispatcher dispatcher;
        CancellationTokenSource stopSendingHeartbeatsTokenSource;
        string endpointName;
        TimeSpan ttlTimeSpan;
        TimeSpan heartbeatInterval;
        TimeSpan registrationRetryInterval = TimeSpan.FromMinutes(1);
        HostInformation hostInfo;

        static ILog Logger = LogManager.GetLogger(typeof(HeartbeatSender));
    }
}