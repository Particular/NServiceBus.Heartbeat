namespace ServiceControl.Plugin.Nsb6.Heartbeat.AcceptanceTests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_service_control_queue_unavailable_at_startup
    {
        [Test]
        public async Task Should_not_fail_the_endpoint()
        {
            var testContext = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithMissingSCQueue>(b => b
                    .CustomConfig((busConfig, context) => busConfig
                        .DefineCriticalErrorAction(c =>
                        {
                            context.CriticalExceptionReceived = true;
                            return Task.FromResult(0);
                        })))
                .Run();

            Assert.IsFalse(testContext.CriticalExceptionReceived);
        }

        class EndpointWithMissingSCQueue : EndpointConfigurationBuilder
        {
            public EndpointWithMissingSCQueue()
            {
                EndpointSetup<DefaultServer>(c => c.HeartbeatPlugin("invalidSCQueue"));
            }
        }

        class Context : ScenarioContext
        {
            public bool CriticalExceptionReceived { get; set; }
        }
    }
}