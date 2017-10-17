namespace ServiceControl.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NServiceBus.SagaAudit;
    using NServiceBus.Transport;
    using SimpleJson;

    class ServiceControlBackend
    {
        public ServiceControlBackend(string destinationQueue, string localAddress)
        {
            this.destinationQueue = destinationQueue;
            this.localAddress = localAddress;
        }

        public Task Send(object messageToSend, TimeSpan timeToBeReceived, IDispatchMessages dispatcher)
        {
            var body = Serialize(messageToSend);
            return Send(body, messageToSend.GetType().FullName, timeToBeReceived, dispatcher);
        }

        internal static byte[] Serialize(object messageToSend)
        {
            return Encoding.UTF8.GetBytes(SimpleJson.SerializeObject(messageToSend, serializerStrategy));
        }

        Task Send(byte[] body, string messageType, TimeSpan timeToBeReceived, IDispatchMessages dispatcher)
        {
            var headers = new Dictionary<string, string>();
            headers[Headers.EnclosedMessageTypes] = messageType;
            headers[Headers.ContentType] = ContentTypes.Json;
            headers[Headers.ReplyToAddress] = localAddress;
            headers[Headers.MessageIntent] = sendIntent;

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), deliveryConstraints: new List<DeliveryConstraint>
            {
                new DiscardIfNotReceivedBefore(timeToBeReceived)
            });
            return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
        }
        
        readonly string sendIntent = MessageIntentEnum.Send.ToString();
        string destinationQueue;
        string localAddress;

        static IJsonSerializerStrategy serializerStrategy = new MessageSerializationStrategy();
    }
}