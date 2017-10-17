namespace ServiceControl.Plugin.Heartbeat.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    class RegisterEndpointStartup
    {
        [DataMember]
        public Guid HostId { get; set; }

        [DataMember]
        public string Endpoint { get; set; }

        [DataMember]
        public DateTime StartedAt { get; set; }

        [DataMember]
        public Dictionary<string, string> HostProperties { get; set; }

        [DataMember]
        public string HostDisplayName { get; set; }

        [DataMember]
        public string Host { get; set; }
    }
}