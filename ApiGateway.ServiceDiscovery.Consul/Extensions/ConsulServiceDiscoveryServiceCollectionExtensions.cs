using System.Net.Http.Headers;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ServiceDiscovery.Consul.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddConsulClient(this IServiceCollection services, IConfigurationSection consulClientConfigurationSection)
    {
        ConsulClientOptions consulClientOptions = new();
        consulClientConfigurationSection.Bind(consulClientOptions);

        return services.AddConsulClient(consulClientOptions);
    }

    public static IServiceCollection AddConsulClient(this IServiceCollection services, ConsulClientOptions consulClientOptions)
    {
        _ = consulClientOptions ?? throw new ArgumentException("Invalid ConsulClientOptions");
        _ = consulClientOptions.Host ?? throw new ArgumentException("Invalid Consul client host");

        services.AddHttpClient("Consul", client =>
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var consulClientConfiguration = new ConsulClientConfiguration
        {
            Address = new Uri(consulClientOptions.Host),
            Datacenter = consulClientOptions.Datacenter
        };

        services.TryAddTransient<IConsulClient>(sp =>
        {
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new ConsulClient(consulClientConfiguration, clientFactory.CreateClient("Consul"));
        });

        return services;
    }
}
