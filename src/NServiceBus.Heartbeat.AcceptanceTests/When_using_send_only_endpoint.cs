namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System.Collections.Generic;
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
        public void Should_not_include_reply_to_header()
        {
            var testContext = Scenario.Define<Context>()
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
            public IDictionary<string, string> Headers { get; set; }
        }

        class HeartbeatEndpoint : EndpointConfigurationBuilder
        {
            public HeartbeatEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendHeartbeatTo(EndpointName);
                }).SendOnly();

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
                public IBus Bus { get; set; }

                public void Handle(RegisterEndpointStartup message)
                {
                    Context.Headers = Bus.CurrentMessageContext.Headers;
                    Context.RegisterMessage = message;
                }
            }

            public class HeartbeatHandler : IHandleMessages<EndpointHeartbeat>
            {
                public Context Context { get; set; }

                public void Handle(EndpointHeartbeat message)
                {
                }
            }
        }
    }
}