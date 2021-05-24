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
        public HeartbeatSender(IMessageDispatcher dispatcher, HostInformation hostInfo, ServiceControlBackend backend, string endpointName, TimeSpan interval, TimeSpan timeToLive)
        {
            this.dispatcher = dispatcher;
            this.hostInfo = hostInfo;
            this.backend = backend;
            this.endpointName = endpointName;
            this.interval = interval;
            this.timeToLive = timeToLive;
        }

        public void Dispose() => stopSendingTokenSource?.Dispose();

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            stopSendingTokenSource = new CancellationTokenSource();

            // don't block here since StartupTasks are executed synchronously.
            _ = SendEndpointStartupMessageAndSwallowExceptions(DateTime.UtcNow, default, true, stopSendingTokenSource.Token);

            _ = SendHeartbeatsAndSwallowExceptions(stopSendingTokenSource.Token);

            return Task.CompletedTask;
        }

        async Task SendHeartbeatsAndSwallowExceptions(CancellationToken cancellationToken)
        {
            Logger.Debug($"Start sending heartbeats every {interval}");

            while (true)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

                    var message = new EndpointHeartbeat { ExecutedAt = DateTime.UtcNow, EndpointName = endpointName, Host = hostInfo.DisplayName, HostId = hostInfo.HostId };

                    await backend.Send(message, timeToLive, dispatcher, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    Logger.Debug("Heartbeat sending canceled.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Warn("Unable to send heartbeat to ServiceControl.", ex);
                }
            }
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            stopSendingTokenSource?.Cancel();

            return Task.CompletedTask;
        }

        async Task SendEndpointStartupMessageAndSwallowExceptions(DateTime startupTime, TimeSpan delay, bool retry, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                var message = new RegisterEndpointStartup
                {
                    HostId = hostInfo.HostId,
                    Host = hostInfo.DisplayName,
                    Endpoint = endpointName,
                    HostDisplayName = hostInfo.DisplayName,
                    HostProperties = hostInfo.Properties,
                    StartedAt = startupTime,
                };

                await backend.Send(message, timeToLive, dispatcher, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                Logger.Debug("Heartbeat sending canceled.", ex);
                return;
            }
            catch (Exception ex)
            {
                if (retry)
                {
                    Logger.Warn($"Unable to register endpoint startup with ServiceControl. Going to reattempt registration after {registrationRetryInterval}.", ex);
                    await SendEndpointStartupMessageAndSwallowExceptions(startupTime, registrationRetryInterval, false, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Logger.Warn("Unable to register endpoint startup with ServiceControl.", ex);
                }
            }
        }

        CancellationTokenSource stopSendingTokenSource;

        readonly IMessageDispatcher dispatcher;
        readonly HostInformation hostInfo;
        readonly ServiceControlBackend backend;
        readonly TimeSpan interval;
        readonly TimeSpan timeToLive;
        readonly string endpointName;

        static readonly TimeSpan registrationRetryInterval = TimeSpan.FromMinutes(1);
        static readonly ILog Logger = LogManager.GetLogger<HeartbeatSender>();
    }
}
