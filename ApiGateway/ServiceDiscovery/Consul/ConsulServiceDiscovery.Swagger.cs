using Consul;
using System.Net;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.LoadBalancing;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using ApiGateway.ServiceDiscovery.Abstractions;
using System.Collections.ObjectModel;
using Yarp.ReverseProxy.Swagger;

namespace ApiGateway.ServiceDiscovery.Consul
{
    public partial class ConsulServiceDiscovery : IServiceDiscovery
    {
        private static ReverseProxyDocumentFilterConfig GetSwaggerConfig(IEnumerable<ClusterConfig> clusters)
        {
            var dictionary = clusters.ToDictionary(clusterConfig => clusterConfig.ClusterId, clusterConfig =>
                new ReverseProxyDocumentFilterConfig.Cluster
                {
                    Destinations = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster.Destination>(
                        clusterConfig.Destinations.Select(x =>
                            new KeyValuePair<string, ReverseProxyDocumentFilterConfig.Cluster.Destination>(key: x.Key,
                                value: new ReverseProxyDocumentFilterConfig.Cluster.Destination
                                {
                                    Address = x.Value.Address,
                                    Swaggers = new ReverseProxyDocumentFilterConfig.Cluster.Destination.Swagger[]
                                    {
                                        new()
                                        {
                                            PrefixPath = x.Value.Metadata != null &&
                                                         x.Value.Metadata.ContainsKey("Swagger.PrefixPath")
                                                ? x.Value.Metadata["Swagger.PrefixPath"]
                                                : null,
                                            Paths = x.Value.Metadata != null &&
                                                    x.Value.Metadata.ContainsKey("Swagger.Paths")
                                                ? new Collection<string>() { x.Value.Metadata["Swagger.Paths"] }
                                                : null
                                        }
                                    }
                                })))
                });

            return new ReverseProxyDocumentFilterConfig
            {
                Clusters = dictionary
            };
        }

    }
}
