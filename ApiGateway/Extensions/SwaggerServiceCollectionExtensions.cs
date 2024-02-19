using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Swagger;

namespace Microsoft.Extensions.DependencyInjection;

internal static class SwaggerServiceCollectionExtensions
{
    internal static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
    }

    /// <summary>
    /// Prepare central-swagger based on routes/clusters configurations
    /// </summary>
    /// <param name="app"></param>
    internal static void UseSwaggerIfNotProduction(this WebApplication app)
    {
        //if (app.Environment.IsProduction())
        //    return;

        //app.UseSwagger();

        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
            {
                var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscovery>();
                var jsoned = serviceDiscovery.ExportConfigs();

                var clusters = serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

                var srvs = new List<OpenApiServer>();
                foreach (var cluster in clusters)
                {
                    //var f = cluster.Destinations.First();
                    //var o = new OpenApiServer() { Url = f.Value.Address };
                    ////options.SwaggerEndpoint($"/swagger/{cluster.ClusterId}/swagger.json", cluster.ClusterId);
                    //swaggerDoc.Servers.Add(o);


                    var op = new OpenApiPathItem();
                    op.AddOperation(OperationType.Get, new OpenApiOperation() { Summary = "sss" });
                    swaggerDoc.Paths.Add(cluster.ClusterId, new OpenApiPathItem(op));
                }

                if (!httpRequest.Headers.ContainsKey("X-Forwarded-Host"))
                    return;

                var basePath = "proxy";
                var serverUrl = $"{httpRequest.Scheme}://{httpRequest.Headers["X-Forwarded-Host"]}/{basePath}";
                swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = serverUrl } };
            });
        });

        app.UseSwaggerUI(options =>
        {
            ////var config = app.Services.GetRequiredService<IOptionsMonitor<ReverseProxyDocumentFilterConfig>>().CurrentValue;
            ////foreach (var cluster in config.Clusters)
            ////{
            ////    options.SwaggerEndpoint($"/swagger/{cluster.Key}/swagger.json", cluster.Key);
            ////}


            options.DocumentTitle = "AtiyanSeir API Gateway";
            var serviceDiscovery = app.Services.GetService<IServiceDiscovery>();
            //options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery);

            ////////var clusters = serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

            ////////foreach (var cluster in clusters)
            ////////{
            ////////    options.SwaggerEndpoint($"/swagger/{cluster.ClusterId}/swagger.json", cluster.ClusterId);
            ////////}
        });
    }
}
