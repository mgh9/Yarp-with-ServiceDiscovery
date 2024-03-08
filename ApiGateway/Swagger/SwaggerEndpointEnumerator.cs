using System.Collections;
using Swashbuckle.AspNetCore.SwaggerUI;
using Yarp.ReverseProxy.Configuration;
using Yarp.ServiceDiscovery.Abstractions;

namespace Yarp.Swagger;

public class SwaggerEndpointEnumerator : IEnumerable<UrlDescriptor>
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    public SwaggerEndpointEnumerator(IServiceDiscovery serviceDiscovery, IHttpContextAccessor httpContextAccessor, ILogger logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public IEnumerator<UrlDescriptor> GetEnumerator()
    {
        string currentReverseProxyServerAddress = GetCurrentReverseProxyServerAddress();
        _logger.LogDebug("Reverse proxy server address is : `{address}`", currentReverseProxyServerAddress);

        var routes = _serviceDiscovery?.GetRoutes() ?? new List<RouteConfig>();
        var clusters = _serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();
        _logger.LogDebug("Enumerating {count} clusters to generating swagger documents...", clusters.Count);

        WarnIfNoClusters(clusters);

        foreach (var clusterItem in clusters)
        {
            var firstDestinationCluster = clusterItem.Destinations?.FirstOrDefault();
            if (firstDestinationCluster is null)
            {
                _logger.LogDebug("Cluster `{clusterItem.ClusterId}` has no destination", clusterItem.ClusterId);
                continue;
            }

            var routesOfThisCluster = routes.Where(x => x.ClusterId == clusterItem.ClusterId).ToList();

            // assume there is only one swagger.json for the service
            var swaggerRouteOfThisCluster = routesOfThisCluster.Where(x => x.RouteId.Contains("swagger")).FirstOrDefault();

            if (swaggerRouteOfThisCluster is null)
            {
                _logger.LogWarning("There is no valid swagger route address for this cluster (`{clusterId}`) to generate swagger document", clusterItem.ClusterId);
                continue;
            }

            string swaggerJsonUrlForOriginalServiceViaReverseProxy = MakeSwaggerJsonUrlForOriginalServiceViaReverseProxy(currentReverseProxyServerAddress, swaggerRouteOfThisCluster);
            _logger.LogInformation("Swagger url of original destination for the clusterId `{clusterId}` is: `{apiSwaggerUrl}`", clusterItem.ClusterId, swaggerJsonUrlForOriginalServiceViaReverseProxy);

            yield return new UrlDescriptor
            {
                Name = clusterItem.ClusterId,
                Url = swaggerJsonUrlForOriginalServiceViaReverseProxy
            };
        }
    }

    private static string MakeSwaggerJsonUrlForOriginalServiceViaReverseProxy(string reverseProxyServerAddress, RouteConfig swaggerRouteOfThisCluster)
    {
        return $"{reverseProxyServerAddress}{swaggerRouteOfThisCluster.Match.Path}";
    }

    private void WarnIfNoClusters(IReadOnlyList<ClusterConfig> clusters)
    {
        if (clusters.Count == 0)
        {
            _logger.LogWarning("There is no cluster destinations to generate swagger documents!!!");
        }
    }

    private string GetCurrentReverseProxyServerAddress()
    {
        var host = _httpContextAccessor.HttpContext.Request.Host.ToString();
        var serverAddress = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{host}";

        return serverAddress;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
