using Yarp.ReverseProxy.Configuration;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;

public interface IServiceDiscovery
{
    Task ReloadAsync(CancellationToken cancellationToken);
    string ExportConfigs();
    IReadOnlyList<ClusterConfig> GetClusters();
    IReadOnlyList<RouteConfig> GetRoutes();
}
