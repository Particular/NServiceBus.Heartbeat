namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Transport;

    public class When_not_setting_ttl : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_four_times_the_interval_value_for_ttl()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(builder => builder.CustomConfig((configuration, c) =>
                {
                    configuration.ConfigureTransport<InMemoryTransport>().Queue = c.Queue;
                }))
                .Done(c => c.Queue.Count > 0)
                .Run();

            var message = context.Queue.Dequeue();

            var constraint = message.UnicastTransportOperations.First().Properties;
            Assert.AreEqual(TimeSpan.FromSeconds(40), constraint?.DiscardIfNotReceivedBefore.MaxTime);
        }

        class Context : ScenarioContext
        {
            public Queue<TransportOperations> Queue { get; set; } = new Queue<TransportOperations>();
        }

        class Sender : EndpointConfigurationBuilder
        {

            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport(new InMemoryTransport());
                    c.SendHeartbeatTo("ServiceControl");
                    c.SendOnly();
                });
            }
        }
    }
}