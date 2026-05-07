namespace NServiceBus.AcceptanceTests.EndpointTemplates;

using System.Threading.Tasks;
using AcceptanceTesting.Support;
using NServiceBus.Heartbeat.AcceptanceTests;
using Configuration.AdvancedExtensibility;
using Transport;

public static class ConfigureExtensions
{
    public static RoutingSettings ConfigureRouting(this EndpointConfiguration configuration) =>
        new(configuration.GetSettings());

    // The acceptance testing framework does not expose transport definition access from endpoint setup.
    public static TransportDefinition ConfigureTransport(this EndpointConfiguration configuration) =>
        configuration.GetSettings().Get<TransportDefinition>();

    public static TTransportDefinition ConfigureTransport<TTransportDefinition>(
        this EndpointConfiguration configuration)
        where TTransportDefinition : TransportDefinition =>
        (TTransportDefinition)configuration.GetSettings().Get<TransportDefinition>();

    public static async Task DefineTransport(this EndpointConfiguration config, RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
    {
        if (config.GetSettings().HasSetting<TransportDefinition>())
        {
            return;
        }
        var transportConfiguration = new ConfigureEndpointLearningTransport();
        await transportConfiguration.Configure(config);
        runDescriptor.OnTestCompleted(_ => transportConfiguration.Cleanup());
    }

    public static async Task DefinePersistence(this EndpointConfiguration config, RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration)
    {
        var persistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence();
        await persistenceConfiguration.Configure(config);
        runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());
    }
}
