namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Performance.TimeToBeReceived;
    using Transport;

    public class When_setting_explicit_ttl : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_it_for_check_messages()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.CustomConfig((cfg, ctx) => cfg.GetSettings().Set("InMemQueue", ctx.Queue)))
                .Done(c => c.Queue.Count > 0)
                .Run();

            var message = context.Queue.Dequeue();

            var constraint = message.UnicastTransportOperations.First().DeliveryConstraints.OfType<DiscardIfNotReceivedBefore>().First();
            Assert.AreEqual(TimeSpan.FromSeconds(20), constraint.MaxTime);
        }

        class Context : ScenarioContext
        {
            public Queue<TransportOperations> Queue { get; } = new Queue<TransportOperations>();
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendHeartbeatTo("ServiceControl", TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(20));
                    c.UseTransport<InMemoryTransport>();
                    c.SendOnly();
                });
            }
        }
    }
}