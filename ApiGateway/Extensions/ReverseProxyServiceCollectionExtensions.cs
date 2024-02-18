using ApiGateway.ServiceDiscovery.Consul;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Swagger.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ReverseProxyServiceCollectionExtensions
{
    internal static void AddCustomReverseProxy(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        services.AddReverseProxy()
                    .ConfigureHttpClient((context, handler) =>
                    {
                        //if (builder.Environment.IsDevelopment())
                        {
                            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, chainErrors) => true;
                        }
                    })
                    .LoadFromConsul()
                    .AddSwagger(EmptyReverseProxyDocumentFilterConfig);
    }

    private static ReverseProxyDocumentFilterConfig EmptyReverseProxyDocumentFilterConfig => new()
    {
        Clusters = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster>(),
        Routes = new Dictionary<string, RouteConfig>()
    };


    /// <summary>
    /// Prepare central-swagger based on routes/clusters configurations
    /// </summary>
    /// <param name="app"></param>
    internal static void UseSwaggerIfNotProduction(this WebApplication app)
    {
        if (app.Environment.IsProduction())
            return;

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "AtiyanSeir API Gateway";
            var serviceDiscovery = app.Services.GetService<IServiceDiscovery>();
            options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery);

            var clusters = serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

            foreach (var cluster in clusters)
            {
                options.SwaggerEndpoint($"/swagger/{cluster.ClusterId}/swagger.json", cluster.ClusterId);
            }
        });
    }
}

