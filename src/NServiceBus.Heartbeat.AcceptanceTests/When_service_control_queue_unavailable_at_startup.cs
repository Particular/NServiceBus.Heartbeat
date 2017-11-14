namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_service_control_queue_unavailable_at_startup
    {
        [Test]
        public void Should_not_fail_the_endpoint()
        {
            var testContext = new Context();
            Scenario.Define(testContext)
                .WithEndpoint<EndpointWithMissingSCQueue>(b => b
                    .CustomConfig(busConfig => busConfig
                        .DefineCriticalErrorAction((m, e) =>
                        {
                            testContext.CriticalExceptionReceived = true;
                        })))
                .AllowExceptions()
                .Run();

            Assert.IsFalse(testContext.CriticalExceptionReceived);
        }

        class EndpointWithMissingSCQueue : EndpointConfigurationBuilder
        {
            public EndpointWithMissingSCQueue()
            {
                EndpointSetup<DefaultServer>(c => c.SendHeartbeatTo("invalidSCQueue"));
            }
        }

        class Context : ScenarioContext
        {
            public bool CriticalExceptionReceived { get; set; }
        }
    }
}