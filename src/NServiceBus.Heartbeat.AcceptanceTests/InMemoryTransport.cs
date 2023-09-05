namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class InMemoryTransport : TransportDefinition
    {
        public Queue<TransportOperations> Queue { get; set; } = new Queue<TransportOperations>();

        class InMemTransportInfrastructure : TransportInfrastructure
        {
            InMemoryTransport transport;

            public InMemTransportInfrastructure(InMemoryTransport transport) => this.transport = transport;

            public void Initialize() => Dispatcher = new InMemoryDispatcher(transport.Queue);

            class InMemoryDispatcher : IMessageDispatcher
            {
                Queue<TransportOperations> queue;

                public InMemoryDispatcher(Queue<TransportOperations> queue) => this.queue = queue;

                public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
                {
                    queue.Enqueue(outgoingMessages);
                    return Task.FromResult(0);
                }
            }

            public override Task Shutdown(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public override string ToTransportAddress(QueueAddress address) => address.ToString();
        }

        public InMemoryTransport() : base(TransportTransactionMode.None, true, true, true)
        {
        }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers,
            string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            var infrastructure = new InMemTransportInfrastructure(this);
            infrastructure.Initialize();

            return Task.FromResult<TransportInfrastructure>(infrastructure);
        }


        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
            new[] { TransportTransactionMode.None };

        [Obsolete("Inject the ITransportAddressResolver type to access the address translation mechanism at runtime. See the NServiceBus version 8 upgrade guide for further details.")]
        public override string ToTransportAddress(QueueAddress address) => throw new NotImplementedException();
    }
}