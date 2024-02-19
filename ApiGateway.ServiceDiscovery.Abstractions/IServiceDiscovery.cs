using Yarp.ReverseProxy.Configuration;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;

public interface IServiceDiscovery
{
    Task ReloadRoutesAndClustersAsync(CancellationToken cancellationToken);

    IReadOnlyList<ClusterConfig> GetClusters();

    IReadOnlyList<RouteConfig> GetRoutes();

    string ExportConfigs();
}
