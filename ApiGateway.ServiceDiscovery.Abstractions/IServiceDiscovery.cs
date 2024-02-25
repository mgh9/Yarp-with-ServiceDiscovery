using Yarp.ReverseProxy.Configuration;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;

public interface IServiceDiscovery
{
    Task ReloadRoutesAndClustersAsync(CancellationToken cancellationToken);
    string ExportRoutesAndClustersAsJson();
    IReadOnlyList<ClusterConfig> GetClusters();
    IReadOnlyList<RouteConfig> GetRoutes();
}
