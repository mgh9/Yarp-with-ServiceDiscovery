using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;
using static AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.ConsulServiceDiscoveryOptions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceDiscoveryRegistryServiceCollectionExtensions
{
    public static IServiceCollection RegisterWithConsulServiceDiscovery(this IServiceCollection services, ConsulClientOptions consulClientOptions)
    {
        services.AddConsulClient(consulClientOptions);
        services.AddHostedService<ConsulRegistrationBackgroundService>();

        return services;
    }
}
