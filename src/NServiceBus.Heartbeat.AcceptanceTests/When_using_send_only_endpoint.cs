namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.Heartbeat.Messages;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_using_send_only_endpoint
    {
        static string EndpointName => Conventions.EndpointNamingConvention(typeof(FakeServiceControl));

        [Test]
        public async Task Should_not_include_reply_to_header()
        {
            var testContext = await Scenario.Define<Context>()
                .WithEndpoint<HeartbeatEndpoint>()
                .WithEndpoint<FakeServiceControl>()
                .Done(c => c.RegisterMessage != null)
                .Run();

            Assert.NotNull(testContext.RegisterMessage);
            Assert.False(testContext.Headers.ContainsKey(Headers.ReplyToAddress));
        }

        class Context : ScenarioContext
        {
            public RegisterEndpointStartup RegisterMessage { get; set; }
            public IReadOnlyDictionary<string, string> Headers { get; set; }
        }

        class HeartbeatEndpoint : EndpointConfigurationBuilder
        {
            public HeartbeatEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendHeartbeatTo(EndpointName);
                    c.SendOnly();
                });

                IncludeType<EndpointHeartbeat>();
                IncludeType<RegisterEndpointStartup>();
            }

        }

        class FakeServiceControl : EndpointConfigurationBuilder
        {
            public FakeServiceControl()
            {
                IncludeType<EndpointHeartbeat>();
                IncludeType<RegisterEndpointStartup>();
                EndpointSetup<DefaultServer>();
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
                    scenarioContext.Headers = context.MessageHeaders;
                    scenarioContext.RegisterMessage = message;
                    return Task.FromResult(0);
                }
            }

            public class HeartbeatHandler : IHandleMessages<EndpointHeartbeat>
            {
                public Task Handle(EndpointHeartbeat message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }
}