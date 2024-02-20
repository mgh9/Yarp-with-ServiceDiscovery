using System;
using System.Collections;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Swashbuckle.AspNetCore.SwaggerUI;
using Yarp.ReverseProxy.Configuration;

namespace AtiyanSeir.B2B.ApiGateway.Swagger;

public class SwaggerEndpointEnumerator : IEnumerable<UrlDescriptor>
{
    private readonly IServiceDiscovery _serviceDiscovery;

    public SwaggerEndpointEnumerator(IServiceDiscovery serviceDiscovery)
    {
        _serviceDiscovery = serviceDiscovery;
    }

    public IEnumerator<UrlDescriptor> GetEnumerator()
    {
        var routes = _serviceDiscovery?.GetRoutes() ?? new List<RouteConfig>();
        var clusters = _serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

        var jsoned = _serviceDiscovery?.ExportConfigs();

        foreach (var clusterItem in clusters)
        {
            var firstDest = clusterItem.Destinations.First();

            var routesOfThisCluster = routes.Where(x => x.ClusterId == clusterItem.ClusterId).ToList();
            var mainRouteOfThisCluster = routesOfThisCluster.Where(x => x.RouteId.Contains("swagger") == false).FirstOrDefault();
            var swaggerRouteOfThisCluster = routesOfThisCluster.Where(x => x.RouteId.Contains("swagger")).FirstOrDefault();

            var swaggerUrlViaGateway1 = "https://192.168.12.112:7219" + swaggerRouteOfThisCluster.Match.Path;
            var swaggerUrlViaGateway2 = $"https://192.168.12.112:7219{mainRouteOfThisCluster.Match.Path.Replace("/{**remainder}", "")}{swaggerRouteOfThisCluster.Match.Path}";


            yield return new UrlDescriptor
            {
                Name = clusterItem.ClusterId,
                Url = swaggerUrlViaGateway1
                //Url = firstDest.Value.Address + "/swagger/v1/swagger.json" //"https://localhost:7094/swagger/v1/swagger.json"
            };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
