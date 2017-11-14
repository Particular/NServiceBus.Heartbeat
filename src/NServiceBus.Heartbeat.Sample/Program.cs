using System;
using NServiceBus;

class Program
{
    static void Main()
    {
	    Console.Title = "NServiceBus.Heartbeat.Sample";
        var busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName("NServiceBus.Heartbeat.Sample");

        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.SendHeartbeatTo("Particular.ServiceControl");

        using (Bus.CreateSendOnly(busConfiguration))
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
