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

        foreach (var clusterItem in clusters)
        {
            var firstDest = clusterItem.Destinations.First();

            yield return new UrlDescriptor
            {
                Name = clusterItem.ClusterId,
                Url = firstDest.Value.Address + "/swagger/v1/swagger.json" //"https://localhost:7094/swagger/v1/swagger.json"
            };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
