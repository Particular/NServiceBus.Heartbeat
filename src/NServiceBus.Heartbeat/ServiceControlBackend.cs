namespace NServiceBus.Heartbeat
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Routing;
    using Transport;

    class ServiceControlBackend
    {
        public ServiceControlBackend(string destinationQueue, ReceiveAddresses receiveAddresses)
        {
            this.destinationQueue = destinationQueue;
            this.receiveAddresses = receiveAddresses;
        }

        public Task Send(object messageToSend, TimeSpan timeToBeReceived, IMessageDispatcher dispatcher, CancellationToken cancellationToken = default)
        {
            var body = Serialize(messageToSend);
            return Send(body, messageToSend.GetType().FullName, timeToBeReceived, dispatcher, cancellationToken);
        }

        internal static byte[] Serialize(object messageToSend) => JsonSerializer.SerializeToUtf8Bytes(messageToSend);

        Task Send(byte[] body, string messageType, TimeSpan timeToBeReceived, IMessageDispatcher dispatcher, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.EnclosedMessageTypes] = messageType,
                [Headers.ContentType] = ContentTypes.Json,
                [Headers.MessageIntent] = sendIntent
            };

            if (receiveAddresses != null)
            {
                headers[Headers.ReplyToAddress] = receiveAddresses.MainReceiveAddress;
            }

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var properties = new DispatchProperties { DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived) };
            var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue), properties);
            return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
        }

        readonly string sendIntent = MessageIntent.Send.ToString();
        readonly string destinationQueue;
        readonly ReceiveAddresses receiveAddresses; // note that ReceiveAddresses will be null on send-only endpoints
    }
}