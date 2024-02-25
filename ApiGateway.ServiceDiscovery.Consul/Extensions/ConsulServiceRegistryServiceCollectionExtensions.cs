using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Options;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceRegistryServiceCollectionExtensions
{
    public static IServiceCollection RegisterWithConsulServiceDiscovery(this IServiceCollection services, ConsulClientOptions consulClientOptions)
    {
        services.AddConsulClient(consulClientOptions);
        services.AddHostedService<ConsulRegistrationBackgroundService>();

        return services;
    }
}
