using System;
using NServiceBus;

public class Program
{
    public static void Main()
    {
        var busConfiguration = new BusConfiguration();

        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.SendHeartbeatTo("Particular.ServiceControl");

        using (Bus.CreateSendOnly(busConfiguration))
        {
            Console.Out.WriteLine("Press a key to quit bus");
            Console.ReadKey();
        }
    }
}
