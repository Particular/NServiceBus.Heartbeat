namespace NServiceBus.Heartbeat
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Routing;
    using SimpleJson;
    using Transport;

    class ServiceControlBackend
    {
        public ServiceControlBackend(string destinationQueue, string localAddress)
        {
            this.destinationQueue = destinationQueue;
            this.localAddress = localAddress;
        }

        public Task Send(object messageToSend, TimeSpan timeToBeReceived, IMessageDispatcher dispatcher, CancellationToken cancellationToken = default)
        {
            var body = Serialize(messageToSend);
            return Send(body, messageToSend.GetType().FullName, timeToBeReceived, dispatcher, cancellationToken);
        }

        internal static byte[] Serialize(object messageToSend)
        {
            return Encoding.UTF8.GetBytes(SimpleJson.SerializeObject(messageToSend, serializerStrategy));
        }

        Task Send(byte[] body, string messageType, TimeSpan timeToBeReceived, IMessageDispatcher dispatcher, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.EnclosedMessageTypes] = messageType,
                [Headers.ContentType] = ContentTypes.Json,
                [Headers.MessageIntent] = sendIntent
            };

            if (localAddress != null)
            {
                headers[Headers.ReplyToAddress] = localAddress;
            }

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var properties = new DispatchProperties { DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived) };
            var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), properties);
            return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
        }

        readonly string sendIntent = MessageIntent.Send.ToString();
        string destinationQueue;
        string localAddress;

        static IJsonSerializerStrategy serializerStrategy = new MessageSerializationStrategy();
    }
}