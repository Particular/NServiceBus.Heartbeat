﻿namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.Heartbeat.Messages;

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
                    c.SendHeartbeatTo(EndpointName);
                });

                IncludeType<EndpointHeartbeat>();
                IncludeType<RegisterEndpointStartup>();
            }

            public class RegisterHandler : IHandleMessages<RegisterEndpointStartup>
            {
                readonly Context scenarioContext;
                public RegisterHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(RegisterEndpointStartup message, IMessageHandlerContext context)
                {
                    scenarioContext.RegisterMessage = message;
                    return Task.FromResult(0);
                }
            }

            public class HeartbeatHandler : IHandleMessages<EndpointHeartbeat>
            {
                readonly Context scenarioContext;
                public HeartbeatHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(EndpointHeartbeat message, IMessageHandlerContext context)
                {
                    scenarioContext.HeartbeatMessage = message;
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