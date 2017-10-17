namespace ServiceControl.Plugin.Nsb6.Heartbeat.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using NServiceBus.Settings;
    using NUnit.Framework;
    using Plugin.Heartbeat.Messages;

    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class ServiceControlBackendTests
    {
        [Test]
        public void It_can_serialize_EndpointHeartbeat()
        {
            ConfigurationManager.AppSettings["ServiceControl/Queue"] = "SC";
            var settingsHolder = new SettingsHolder();
            var backend = new ServiceControlBackend(null, settingsHolder);

            var body = backend.Serialize(new EndpointHeartbeat
            {
                EndpointName = "My.Endpoint",
                ExecutedAt = new DateTime(2016, 02, 01, 13, 59, 0),
                Host = "Host",
                HostId = Guid.Empty
            });
            Approvals.Verify(Encoding.UTF8.GetString(body));
        }

        [Test]
        public void It_can_serialize_RegisterEndpointStartup()
        {
            ConfigurationManager.AppSettings["ServiceControl/Queue"] = "SC";
            var settingsHolder = new SettingsHolder();
            var backend = new ServiceControlBackend(null, settingsHolder);

            var body = backend.Serialize(new RegisterEndpointStartup
            {
                HostDisplayName = "Display name :-)",
                Endpoint = "My.Endpoint",
                HostProperties = new Dictionary<string, string>
                {
                    {"key1", "value1"},
                    {"key2", "value2"}
                },
                StartedAt = new DateTime(2016, 02, 01, 13, 59, 0),
                Host = "Host",
                HostId = Guid.Empty
            });
            Approvals.Verify(Encoding.UTF8.GetString(body));
        }
    }
}