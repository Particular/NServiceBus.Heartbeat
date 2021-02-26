using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static void Main()
    {
        AsyncMain().GetAwaiter().GetResult();
    }

    static async Task AsyncMain()
    {
        Console.Title = "NServiceBus.Heartbeat.Sample";
        var endpointConfiguration = new EndpointConfiguration("NServiceBus.Heartbeat.Sample");
        endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
        endpointConfiguration.UseTransport(new LearningTransport());
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.SendHeartbeatTo("Particular.ServiceControl");

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}
