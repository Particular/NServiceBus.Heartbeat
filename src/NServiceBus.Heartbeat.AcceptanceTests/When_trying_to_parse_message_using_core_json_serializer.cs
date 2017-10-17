namespace ServiceControl.Plugin.Nsb6.Heartbeat.AcceptanceTests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Plugin.Heartbeat.Messages;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    public class When_trying_to_parse_message_using_core_json_serializer
    {
        static string EndpointName => Conventions.EndpointNamingConvention(typeof(HeartbeatEndpoint));

        [Test]
        public async Task Should_not_fail()
        {
            var testContext = await Scenario.Define<Context>()
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
                    c.AddDeserializer<JsonSerializer>();
                    c.SendHeartbeatTo(EndpointName);
                });

                IncludeType<EndpointHeartbeat>();
                IncludeType<RegisterEndpointStartup>();
            }

            public class RegisterHandler : IHandleMessages<RegisterEndpointStartup>
            {
                public Context Context { get; set; }

                public Task Handle(RegisterEndpointStartup message, IMessageHandlerContext context)
                {
                    Context.RegisterMessage = message;
                    return Task.FromResult(0);
                }
            }

            public class HeartbeatHandler : IHandleMessages<EndpointHeartbeat>
            {
                public Context Context { get; set; }

                public Task Handle(EndpointHeartbeat message, IMessageHandlerContext context)
                {
                    Context.HeartbeatMessage = message;
                    return Task.FromResult(0);
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