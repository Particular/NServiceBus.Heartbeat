namespace ServiceControl.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Heartbeat.Messages;
    using NServiceBus;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    class ServiceControlBackend
    {
        public ServiceControlBackend(IDispatchMessages messageSender, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.messageSender = messageSender;

            startupSerializer = new DataContractJsonSerializer(typeof(RegisterEndpointStartup), new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("o"),
                EmitTypeInformation = EmitTypeInformation.Never,
                UseSimpleDictionaryFormat = true
            });
            heartbeatSerializer = new DataContractJsonSerializer(typeof(EndpointHeartbeat), new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("o"),
                EmitTypeInformation = EmitTypeInformation.Never,
                UseSimpleDictionaryFormat = true
            });

            serviceControlBackendAddress = GetServiceControlAddress();
        }

        Task Send(byte[] body, string messageType, TimeSpan timeToBeReceived)
        {
            var headers = new Dictionary<string, string>();
            headers[Headers.EnclosedMessageTypes] = messageType;
            headers[Headers.ContentType] = ContentTypes.Json; //Needed for ActiveMQ transport
            headers[Headers.ReplyToAddress] = settings.LocalAddress();
            headers[Headers.MessageIntent] = sendIntent;

            var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(serviceControlBackendAddress), deliveryConstraints: new List<DeliveryConstraint>
            {
                new DiscardIfNotReceivedBefore(timeToBeReceived)
            });
            return messageSender.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
        }

        internal byte[] Serialize(EndpointHeartbeat message)
        {
            return Serialize(message, heartbeatSerializer);
        }

        internal byte[] Serialize(RegisterEndpointStartup message)
        {
            return Serialize(message, startupSerializer);
        }

        static byte[] Serialize(object result, XmlObjectSerializer serializer)
        {
            byte[] body;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, result);
                body = stream.ToArray();
            }

            //hack to remove the type info from the json
            var bodyString = Encoding.UTF8.GetString(body);

            var toReplace = $", {result.GetType().Assembly.GetName().Name}";

            bodyString = bodyString.Replace(toReplace, ", ServiceControl");

            body = Encoding.UTF8.GetBytes(bodyString);
            return body;
        }

        public Task Send(EndpointHeartbeat messageToSend, TimeSpan timeToBeReceived)
        {
            var body = Serialize(messageToSend);
            return Send(body, messageToSend.GetType().FullName, timeToBeReceived);
        }

        public Task Send(RegisterEndpointStartup messageToSend, TimeSpan timeToBeReceived)
        {
            var body = Serialize(messageToSend);
            return Send(body, messageToSend.GetType().FullName, timeToBeReceived);
        }

        string GetServiceControlAddress()
        {
            var queueName = ConfigurationManager.AppSettings["ServiceControl/Queue"];

            if (!string.IsNullOrEmpty(queueName))
            {
                return queueName;
            }

            if (settings.HasSetting("ServiceControl.Queue"))
            {
                queueName = settings.Get<string>("ServiceControl.Queue");
            }

            if (!string.IsNullOrEmpty(queueName))
            {
                return queueName;
            }

            const string errMsg = @"You have ServiceControl plugins installed in your endpoint, however, the Particular ServiceControl queue is not specified.
Please ensure that the Particular ServiceControl queue is specified either via code (config.HeartbeatPlugin(servicecontrolQueue)) or AppSettings (eg. <add key=""ServiceControl/Queue"" value=""particular.servicecontrol@machine""/>).";

            throw new Exception(errMsg);
        }

        IDispatchMessages messageSender;
        readonly string sendIntent = MessageIntentEnum.Send.ToString();
        DataContractJsonSerializer startupSerializer;
        DataContractJsonSerializer heartbeatSerializer;
        string serviceControlBackendAddress;
        ReadOnlySettings settings;
    }
}