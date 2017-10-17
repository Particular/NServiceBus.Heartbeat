namespace ServiceControl.Plugin.Heartbeat.Messages
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    class EndpointHeartbeat
    {
        [DataMember]
        public DateTime ExecutedAt { get; set; }

        [DataMember]
        public string EndpointName { get; set; }

        [DataMember]
        public Guid HostId { get; set; }

        [DataMember]
        public string Host { get; set; }
    }
}