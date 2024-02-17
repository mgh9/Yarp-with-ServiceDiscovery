using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.ServiceDiscovery.Abstractions
{
    public interface IServiceDiscovery
    {
        Task ReloadRoutesAndClustersAsync(CancellationToken stoppingToken);

        IReadOnlyList<ClusterConfig> GetClusters();

        IReadOnlyList<RouteConfig> GetRoutes();
    }
}
