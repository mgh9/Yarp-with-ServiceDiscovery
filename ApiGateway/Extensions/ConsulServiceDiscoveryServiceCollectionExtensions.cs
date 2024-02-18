using System.Net.Http.Headers;
using ApiGateway.Workers;
using Consul;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConsulServiceDiscoveryServiceCollectionExtensions
    {
        public static IServiceCollection AddConsulClient(this IServiceCollection services, IConfigurationSection config)
        {
            services.AddHttpClient("Consul", client =>
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            });

            var host = config.GetValue<string>("Host") ?? throw new ArgumentException("Invalid Consul server address or not found!");
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

        public static IReverseProxyBuilder LoadFromConsul(this IReverseProxyBuilder builder)
        {
            builder.LoadFromMemory(default, default);

            builder.Services.AddHostedService<ConsulMonitorWorker>();

            return builder;
        }
    }
}
