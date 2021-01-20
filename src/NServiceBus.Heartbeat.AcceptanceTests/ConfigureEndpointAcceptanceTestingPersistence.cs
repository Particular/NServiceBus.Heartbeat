namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System.Threading.Tasks;

    public class ConfigureEndpointAcceptanceTestingPersistence
    {
        public Task Configure(EndpointConfiguration configuration)
        {
            configuration.UsePersistence<AcceptanceTestingPersistence>();
            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            // Nothing required for in-memory persistence
            return Task.FromResult(0);
        }
    }
}
