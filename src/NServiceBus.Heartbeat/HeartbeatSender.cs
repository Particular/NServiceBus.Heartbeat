namespace NServiceBus.Heartbeat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Config;
    using Hosting;
    using Logging;
    using ServiceControl.Plugin.Heartbeat.Messages;
    using ServiceControl.Plugin.Nsb6.Heartbeat;
    using Transports;
    using Unicast;

    class HeartbeatSender : IWantToRunWhenConfigurationIsComplete, IDisposable
    {
        const int MillisecondsToWaitForShutdown = 500;
        static ILog Logger = LogManager.GetLogger(typeof(HeartbeatSender));

        public HeartbeatSender(ISendMessages dispatcher, Configure configure, UnicastBus unicastBus)
        {
            var settings = configure.Settings;
            if (!settings.TryGet("NServiceBus.Heartbeat.Queue", out string destinationQueue))
            {
                return; //HB not configured
            }
            if (!settings.TryGet("NServiceBus.Heartbeat.Interval", out heartbeatInterval))
            {
                heartbeatInterval = TimeSpan.FromSeconds(10);
            }
            if (!settings.TryGet("NServiceBus.Heartbeat.Ttl", out ttlTimeSpan))
            {
                ttlTimeSpan = TimeSpan.FromTicks(heartbeatInterval.Ticks * 4);
            }

            var replyToAddress = !settings.GetOrDefault<bool>("Endpoint.SendOnly")
                ? settings.LocalAddress()
                : null;

            endpointName = settings.EndpointName();
            backend = new ServiceControlBackend(dispatcher, Address.Parse(destinationQueue), replyToAddress);
            this.unicastBus = unicastBus;
        }

        public void Dispose()
        {
            if (heartbeatTimer != null)
            {
                using (var manualResetEvent = new ManualResetEvent(false))
                {
                    heartbeatTimer.Dispose(manualResetEvent);
                    manualResetEvent.WaitOne(MillisecondsToWaitForShutdown);
                }
            }

            cancellationTokenSource?.Cancel();
        }

        public void Run(Configure config)
        {
            if (backend == null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            NotifyEndpointStartup(unicastBus.HostInformation, DateTime.UtcNow);
        }


        void NotifyEndpointStartup(HostInformation hostInfo, DateTime startupTime)
        {
            // don't block here since StartupTasks are executed synchronously.
            Task.Run(() => SendEndpointStartupMessage(hostInfo, startupTime, cancellationTokenSource.Token)).Ignore();
        }

        void SendEndpointStartupMessage(HostInformation hostInfo, DateTime startupTime, CancellationToken cancellationToken)
        {
            try
            {
                backend.Send(
                    new RegisterEndpointStartup
                    {
                        HostId = hostInfo.HostId,
                        Host = hostInfo.DisplayName,
                        Endpoint = endpointName,
                        HostDisplayName = hostInfo.DisplayName,
                        HostProperties = hostInfo.Properties,
                        StartedAt = startupTime
                    }, ttlTimeSpan);
                StartHeartbeats(unicastBus.HostInformation);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Unable to register endpoint startup with ServiceControl. Going to reattempt registration after {registrationRetryInterval}.", ex);

                Task.Delay(registrationRetryInterval, cancellationToken)
                    .ContinueWith(t => SendEndpointStartupMessage(hostInfo, startupTime, cancellationToken), cancellationToken)
                    .Ignore();
            }
        }

        void StartHeartbeats(HostInformation hostInfo)
        {
            Logger.DebugFormat("Start sending heartbeats every {0}", heartbeatInterval);
            heartbeatTimer = new Timer(x => SendHeartbeatMessage(hostInfo), null, TimeSpan.Zero, heartbeatInterval);
        }

        void SendHeartbeatMessage(HostInformation hostInfo)
        {
            var heartBeat = new EndpointHeartbeat
            {
                ExecutedAt = DateTime.UtcNow,
                EndpointName = endpointName,
                Host = hostInfo.DisplayName,
                HostId = hostInfo.HostId
            };

            try
            {
                backend.Send(heartBeat, ttlTimeSpan);
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Debug("Ignoring object disposed. Likely means we are shutting down:", ex);
            }
            catch (Exception ex)
            {
                Logger.Warn("Unable to send heartbeat to ServiceControl:", ex);
            }
        }

        UnicastBus unicastBus;

        ServiceControlBackend backend;
        CancellationTokenSource cancellationTokenSource;
        string endpointName;
        TimeSpan heartbeatInterval = TimeSpan.FromSeconds(10);
        Timer heartbeatTimer;
        TimeSpan registrationRetryInterval = TimeSpan.FromMinutes(1);
        TimeSpan ttlTimeSpan;
    }
}