namespace ServiceControl.Plugin.Nsb6.Heartbeat.AcceptanceTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Plugin.Heartbeat.Messages;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

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
                public Context Context { get; set; }

                public Task Handle(RegisterEndpointStartup message, IMessageHandlerContext context)
                {
                    Context.Headers = context.MessageHeaders;
                    Context.RegisterMessage = message;
                    return Task.FromResult(0);
                }
            }

            public class HeartbeatHandler : IHandleMessages<EndpointHeartbeat>
            {
                public Context Context { get; set; }

                public Task Handle(EndpointHeartbeat message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }
    }
}