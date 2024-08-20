namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
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
                        .DefineCriticalErrorAction((_, __) =>
                        {
                            context.CriticalExceptionReceived = true;
                            return Task.CompletedTask;
                        })))
                .Run();

            Assert.That(testContext.CriticalExceptionReceived, Is.False);
            Assert.IsTrue(testContext.Logs.Any(x => x.Message.Contains("Unable to register endpoint startup with ServiceControl.")));
        }

        class EndpointWithMissingSCQueue : EndpointConfigurationBuilder
        {
            public EndpointWithMissingSCQueue()
            {
                EndpointSetup<DefaultServer>(c => c.SendHeartbeatTo(new string(Path.GetInvalidPathChars())));
            }
        }

        class Context : ScenarioContext
        {
            public bool CriticalExceptionReceived { get; set; }
        }
    }
}