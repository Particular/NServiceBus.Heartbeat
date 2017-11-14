namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using NServiceBus;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.Heartbeat.Messages;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_trying_to_parse_message_using_core_json_serializer
    {
        static string EndpointName => Conventions.EndpointNamingConvention(typeof(HeartbeatEndpoint));

        [Test]
        public void Should_not_fail()
        {
            var testContext = Scenario.Define<Context>()
                .WithEndpoint<HeartbeatEndpoint>()
                .Done(c => c.RegisterMessage != null && c.HeartbeatMessage != null)
                .Run();

            Assert.NotNull(testContext.RegisterMessage);
            Assert.AreEqual(EndpointName, testContext.RegisterMessage.Endpoint);
            Assert.IsTrue(testContext.RegisterMessage.HostProperties.ContainsKey("Machine"));

            Assert.NotNull(testContext.HeartbeatMessage);
            Assert.AreEqual(EndpointName, testContext.HeartbeatMessage.EndpointName);
        }

        class HeartbeatEndpoint : EndpointConfigurationBuilder
        {
            public HeartbeatEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendHeartbeatTo(EndpointName);
                });

                IncludeType<EndpointHeartbeat>();
                IncludeType<RegisterEndpointStartup>();
            }

            public class RegisterHandler : IHandleMessages<RegisterEndpointStartup>
            {
                public Context Context { get; set; }

                public void Handle(RegisterEndpointStartup message)
                {
                    Context.RegisterMessage = message;
                }
            }

            public class HeartbeatHandler : IHandleMessages<EndpointHeartbeat>
            {
                public Context Context { get; set; }

                public void Handle(EndpointHeartbeat message)
                {
                    Context.HeartbeatMessage = message;
                }
            }
        }

        class Context : ScenarioContext
        {
            public RegisterEndpointStartup RegisterMessage { get; set; }
            public EndpointHeartbeat HeartbeatMessage { get; set; }
        }
    }
}