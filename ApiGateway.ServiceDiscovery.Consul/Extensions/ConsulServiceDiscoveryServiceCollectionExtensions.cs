using System.Net.Http.Headers;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;
using Consul;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.ConsulServiceDiscoveryOptions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddConsulClient(this IServiceCollection services, ConsulClientOptions consulClientOptions)
    {
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

    public static IReverseProxyBuilder LoadFromConsul(this IReverseProxyBuilder builder)
    {
        builder.LoadFromMemory(default, default);
        builder.Services.AddHostedService<ConsulMonitorBackgroundService>();

        return builder;
    }
}
