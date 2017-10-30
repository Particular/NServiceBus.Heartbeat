namespace NServiceBus.Heartbeat
{
    using System;
    using System.Text;
    using SimpleJson;
    using Transports;
    using Unicast;

    class ServiceControlBackend
    {
        public ServiceControlBackend(ISendMessages dispatcher, Address destinationQueue, Address localAddress)
        {
            this.dispatcher = dispatcher;
            this.destinationQueue = destinationQueue;
            this.localAddress = localAddress;
        }
        
        public void Send(object messageToSend, TimeSpan timeToBeReceived)
        {
            var message = new TransportMessage
            {
                TimeToBeReceived = timeToBeReceived,
                Body = Serialize(messageToSend)
            };
            
            message.Headers[Headers.EnclosedMessageTypes] = messageToSend.GetType().FullName;
            message.Headers[Headers.ContentType] = ContentTypes.Json;

            var sendOptions = new SendOptions(destinationQueue) { ReplyToAddress = localAddress };
            dispatcher.Send(message, sendOptions);
        }
        internal static byte[] Serialize(object messageToSend)
        {
            return Encoding.UTF8.GetBytes(SimpleJson.SerializeObject(messageToSend, serializerStrategy));
        }

        static IJsonSerializerStrategy serializerStrategy = new MessageSerializationStrategy();
        ISendMessages dispatcher;
        Address destinationQueue;
        Address localAddress;
    }
}