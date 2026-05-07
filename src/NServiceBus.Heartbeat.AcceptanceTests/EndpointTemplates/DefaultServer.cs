namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.EnableInstallers();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");
            configuration.UseSerialization<SystemJsonSerializer>();

            await configurationBuilderCustomization(configuration);

            await configuration.DefineTransport(runDescriptor, endpointConfiguration).ConfigureAwait(false);

            await configuration.DefinePersistence(runDescriptor, endpointConfiguration).ConfigureAwait(false);

            configuration.ScanTypesForTest(endpointConfiguration);

            return configuration;
        }
    }
}
