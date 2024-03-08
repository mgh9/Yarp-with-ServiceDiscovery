using Yarp.ReverseProxy.Configuration;

namespace Yarp.ServiceDiscovery.Abstractions;

public interface IServiceDiscovery
{
    Task ReloadAsync(CancellationToken cancellationToken);
    string ExportConfigs();
    IReadOnlyList<ClusterConfig> GetClusters();
    IReadOnlyList<RouteConfig> GetRoutes();
}
