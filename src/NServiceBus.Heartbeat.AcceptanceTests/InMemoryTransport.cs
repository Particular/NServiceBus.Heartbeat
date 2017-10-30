namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Extensibility;
    using Performance.TimeToBeReceived;
    using Routing;
    using Settings;
    using Transport;

    class InMemoryTransport : TransportDefinition
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new InMemTransportInfrastructure(settings);
        }

        public override string ExampleConnectionStringForErrorMessage => null;
        public override bool RequiresConnectionString => false;

        class InMemTransportInfrastructure : TransportInfrastructure
        {
            Queue<TransportOperations> queue;

            public InMemTransportInfrastructure(SettingsHolder settings)
            {
                queue = settings.Get<Queue<TransportOperations>>("InMemQueue");
            }

            public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
            {
                throw new NotImplementedException("Only sending is supported");
            }

            public override TransportSendInfrastructure ConfigureSendInfrastructure()
            {
                return new TransportSendInfrastructure(() => new Dispatcher(queue), () => Task.FromResult(StartupCheckResult.Success));
            }

            public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
            {
                throw new NotImplementedException();
            }

            public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
            {
                return instance;
            }

            public override string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return logicalAddress.EndpointInstance.Endpoint;
            }

            public override IEnumerable<Type> DeliveryConstraints => new[]
            {
                typeof(DoNotDeliverBefore),
                typeof(DelayDeliveryWith),
                typeof(DiscardIfNotReceivedBefore)
            };

            public override TransportTransactionMode TransactionMode => TransportTransactionMode.None;
            public override OutboundRoutingPolicy OutboundRoutingPolicy => new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);

            class Dispatcher : IDispatchMessages
            {
                Queue<TransportOperations> queue;

                public Dispatcher(Queue<TransportOperations> queue)
                {
                    this.queue = queue;
                }

                public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
                {
                    queue.Enqueue(outgoingMessages);
                    return Task.FromResult(0);
                }
            }
        }
    }
}