using System.Collections.ObjectModel;
using ApiGateway.Extensions;
using ApiGateway.ServiceDiscovery.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Swagger.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ReverseProxyServiceCollectionExtensions
{
    internal static void AddCustomReverseProxy(this IServiceCollection services, IConfiguration config)
    {

        ////////static ReverseProxyDocumentFilterConfig GetSwaggerConfig(IEnumerable<ClusterConfig> clusters)
        ////////{
        ////////    var dictionary = clusters.ToDictionary(clusterConfig => clusterConfig.ClusterId, clusterConfig =>
        ////////        new ReverseProxyDocumentFilterConfig.Cluster
        ////////        {
        ////////            Destinations = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster.Destination>(
        ////////                clusterConfig.Destinations.Select(x =>
        ////////                    new KeyValuePair<string, ReverseProxyDocumentFilterConfig.Cluster.Destination>(key: x.Key,
        ////////                        value: new ReverseProxyDocumentFilterConfig.Cluster.Destination
        ////////                        {
        ////////                            Address = x.Value.Address,
        ////////                            Swaggers = new ReverseProxyDocumentFilterConfig.Cluster.Destination.Swagger[]
        ////////                            {
        ////////                                new()
        ////////                                {
        ////////                                    PrefixPath = x.Value.Metadata != null &&
        ////////                                                 x.Value.Metadata.ContainsKey("Swagger.PrefixPath")
        ////////                                        ? x.Value.Metadata["Swagger.PrefixPath"]
        ////////                                        : null,
        ////////                                    Paths = x.Value.Metadata != null &&
        ////////                                            x.Value.Metadata.ContainsKey("Swagger.Paths")
        ////////                                        ? new Collection<string>() { x.Value.Metadata["Swagger.Paths"] }
        ////////                                        : null
        ////////                                }
        ////////                            }
        ////////                        })))
        ////////        });

        ////////    return new ReverseProxyDocumentFilterConfig
        ////////    {
        ////////        Clusters = dictionary
        ////////    };
        ////////}

        //////////var serviceDiscoveryKeyValueProvider = services.BuildServiceProvider().GetRequiredService<IServiceDiscoveryKeyValueProvider>();
        //////////var reverseProxy = serviceDiscoveryKeyValueProvider.GetAsync<ReverseProxyOptions>("ReverseProxy", default).Result;

        //////////services.AddReverseProxy()
        //////////        .LoadFromMemory(reverseProxy.Routes, reverseProxy.Clusters)
        //////////        .AddSwagger(GetSwaggerConfig(reverseProxy.Clusters));

        //////////var serviceDiscoveryKeyValueProvider = services.BuildServiceProvider().GetRequiredService<IServiceDiscoveryKeyValueProvider>();
        ////////var serviceProvider = services.BuildServiceProvider();
        ////////var serviceDiscovery = serviceProvider.GetService<IServiceDiscovery>();
        ////////var clusters = serviceDiscovery.GetClusters();

        ////////ReverseProxyDocumentFilterConfig reverseProxyDocumentFilterConfig = GetSwaggerConfig(clusters);
        ////////reverseProxyDocumentFilterConfig = new ReverseProxyDocumentFilterConfig { Clusters = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster>() };
        var x = services.AddReverseProxy()
                        .ConfigureHttpClient((context, handler) =>
                            {
                                //if (builder.Environment.IsDevelopment())
                                {
                                    handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, chainErrors) => true;
                                }
                            })
                            .LoadFromConsul();
        //.AddSwagger(reverseProxyDocumentFilterConfig);
        var reverseProxyDocumentFilterConfig = new ReverseProxyDocumentFilterConfig()
        {
            Clusters = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster>(),
            Routes = new Dictionary<string, RouteConfig>()
        };
        x.AddSwagger(reverseProxyDocumentFilterConfig);
    }


    /// <summary>
    /// Prepare central-swagger based on routes/clusters configurations
    /// </summary>
    /// <param name="app"></param>
    internal static void PrepareSwaggerIfNotProduction(this WebApplication app)
    {
        if (app.Environment.IsProduction())
            return;

        app.UseSwagger();

//        c.ConfigObject.Urls = new SwaggerEndpointEnumerator();

        app.UseSwaggerUI(options =>
        {
            var serviceDiscovery = app.Services.GetService<IServiceDiscovery>();
            options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery);
            //options.ConfigObject.DocExpansion= Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.

            //var serviceDiscoveryKeyValueProvider = app.Services.GetRequiredService<IServiceDiscoveryKeyValueProvider>();
            //var reverseProxy = serviceDiscoveryKeyValueProvider.GetAsync<ReverseProxyOptions>("ReverseProxy", default).Result;
            var clusters = serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

            foreach (var cluster in clusters)
            {
                options.SwaggerEndpoint($"/swagger/{cluster.ClusterId}/swagger.json", cluster.ClusterId);
            }
        });
    }
}

