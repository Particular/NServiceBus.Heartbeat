namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;

    public static class ConfigureExtensions
    {
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
            var persistenceConfiguration = new ConfigureEndpointLearningPersistence();
            await persistenceConfiguration.Configure(config);
            runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());
        }

        public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
        {
            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });
        }

        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IServiceCollection r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.AddSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }
    }
}