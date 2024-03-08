using Consul;
using Microsoft.Extensions.Logging;
using Yarp.ServiceDiscovery.Consul.Options;
using Yarp.ServiceDiscovery.Consul.Workers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceRegistryServiceCollectionExtensions
{
    public static IServiceCollection RegisterWithConsulServiceRegistry(this IServiceCollection services, ConsulServiceRegistryOptions consulServiceRegistryOptions)
    {
        services.AddConsulClient(consulServiceRegistryOptions.ConsulClient);
        services.AddHostedService(opt =>
        {
            var consulClient = opt.GetRequiredService<IConsulClient>();
            var logger = opt.GetService<ILogger<ConsulRegistrationBackgroundService>>();

            return new ConsulRegistrationBackgroundService(consulClient, consulServiceRegistryOptions.ServiceInfo, logger);
        });

        return services;
    }
}
