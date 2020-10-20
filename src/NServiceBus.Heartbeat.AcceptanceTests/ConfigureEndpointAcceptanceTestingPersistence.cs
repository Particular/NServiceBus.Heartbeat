using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting.Support;

namespace NServiceBus.Heartbeat.AcceptanceTests
{
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
