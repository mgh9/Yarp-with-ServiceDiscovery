using System.Net.Http.Headers;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyShared;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsulClient(this IServiceCollection services, IConfigurationSection config)
    {
        services.AddHttpClient("Consul", client =>
        {
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var host = config.GetValue<string>("Host") ?? string.Empty;
        var dc = config.GetValue<string>("Datacenter") ?? string.Empty;

        var consulClientConfiguration = new ConsulClientConfiguration
        {
            Address = new Uri(host),
            Datacenter = dc
        };

        services.TryAddTransient<IConsulClient>(sp =>
        {
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();

            return new ConsulClient(consulClientConfiguration,
                clientFactory.CreateClient("Consul"));
        });

        return services;
    }

    public static IServiceCollection AddServiceDiscoveryConsul(this IServiceCollection services, IConfigurationSection config)
    {
        services.AddConsulClient(config.GetSection("Client"));

        services.AddHostedService<ConsulRegistrationBackgroundService>();

        return services;
    }
}
