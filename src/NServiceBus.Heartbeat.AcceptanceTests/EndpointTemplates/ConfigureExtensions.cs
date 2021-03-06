﻿namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using NServiceBus.Heartbeat.AcceptanceTests;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Transport;

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
            var persistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence();
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

        public static TTransportDefinition ConfigureTransport<TTransportDefinition>(
            this EndpointConfiguration configuration)
            where TTransportDefinition : TransportDefinition =>
            (TTransportDefinition)configuration.GetSettings().Get<TransportDefinition>();
    }
}