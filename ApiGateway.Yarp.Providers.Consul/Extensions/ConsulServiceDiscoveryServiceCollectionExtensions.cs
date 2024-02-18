using System.Net.Http.Headers;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddConsulClient(this IServiceCollection services, IConfigurationSection consulClientConfigSection)
    {
        services.AddHttpClient("Consul", client =>
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var host = consulClientConfigSection.GetValue<string>("Host") ?? throw new ArgumentException("Invalid Consul server address or not found!");
        var dc = consulClientConfigSection.GetValue<string>("Datacenter") ?? string.Empty;

        var consulClientConfiguration = new ConsulClientConfiguration
        {
            Address = new Uri(host),
            Datacenter = dc
        };

        services.TryAddTransient<IConsulClient>(sp =>
        {
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();

            return new ConsulClient(consulClientConfiguration, clientFactory.CreateClient("Consul"));
        });

        //services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        return services;
    }

    public static IReverseProxyBuilder LoadFromConsul(this IReverseProxyBuilder builder)
    {
        builder.LoadFromMemory(default, default);

        builder.Services.AddHostedService<ConsulMonitorBackgroundService>();

        return builder;
    }

    public static IServiceCollection RegisterWithConsulServiceDiscovery(this IServiceCollection services, IConfigurationSection config)
    {
        services.AddConsulClient(config.GetSection("Client"));

        services.AddHostedService<ConsulRegistrationBackgroundService>();

        return services;
    }
}
