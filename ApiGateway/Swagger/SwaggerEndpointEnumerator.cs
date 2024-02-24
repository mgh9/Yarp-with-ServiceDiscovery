using System.Collections;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Swashbuckle.AspNetCore.SwaggerUI;
using Yarp.ReverseProxy.Configuration;

namespace AtiyanSeir.B2B.ApiGateway.Swagger;

public class SwaggerEndpointEnumerator : IEnumerable<UrlDescriptor>
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    public SwaggerEndpointEnumerator(IServiceDiscovery serviceDiscovery, IHttpContextAccessor httpContextAccessor, ILogger logger)
    {
        _serviceDiscovery = serviceDiscovery;
        this._httpContextAccessor = httpContextAccessor;
        this._logger = logger;
    }

    public IEnumerator<UrlDescriptor> GetEnumerator()
    {
        var routes = _serviceDiscovery?.GetRoutes() ?? new List<RouteConfig>();
        var clusters = _serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

        string serverAddress = GetServerAddress();
        _logger.LogDebug("Reverse proxy server address is : `{address}`", serverAddress);

        _logger.LogDebug("Enumerating {count} clusters to generating swagger documents...", clusters.Count);
        foreach (var clusterItem in clusters)
        {
            var firstDestinationCluster = clusterItem.Destinations?.FirstOrDefault();
            if (firstDestinationCluster is null)
            {
                _logger.LogDebug("Cluster `{clusterItem.ClusterId}` has no destination", clusterItem.ClusterId);
                continue;
            }

            var routesOfThisCluster = routes.Where(x => x.ClusterId == clusterItem.ClusterId).ToList();
            var swaggerRouteOfThisCluster = routesOfThisCluster.Where(x => x.RouteId.Contains("swagger")).FirstOrDefault();

            if (swaggerRouteOfThisCluster is null)
            {
                _logger.LogWarning("There is no valid swagger route address for this cluster (`{clusterId}`) to generate swagger document", clusterItem.ClusterId);
                continue;
            }

            var originalApiSwaggerUrlViaServiceProxy = serverAddress + swaggerRouteOfThisCluster.Match.Path;
            _logger.LogDebug("Swagger url of original route for the clusterId `{clusterId}` is valid, swagger url is: `{apiSwaggerUrl}`", clusterItem.ClusterId, originalApiSwaggerUrlViaServiceProxy);

            yield return new UrlDescriptor
            {
                Name = clusterItem.ClusterId,
                Url = originalApiSwaggerUrlViaServiceProxy
            };
        }
    }

    private string GetServerAddress()
    {
        var host = _httpContextAccessor.HttpContext.Request.Host.ToString();
        var serverAddress = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{host}";

        return serverAddress;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
