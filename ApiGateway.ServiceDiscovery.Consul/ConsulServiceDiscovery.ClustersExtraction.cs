using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Consul;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.LoadBalancing;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;

namespace ApiGateway.ServiceDiscovery.Consul;

public partial class ConsulServiceDiscovery : IServiceDiscovery
{
    private async Task<List<ClusterConfig>> ExtractClusters(QueryResult<Dictionary<string, AgentService>> getServicesResult)
    {
        var clusters = new Dictionary<string, ClusterConfig>();
        var serviceNameToItsDataMapping = getServicesResult.Response;

        foreach (var (serviceName, consulService) in serviceNameToItsDataMapping)
        {
            ClusterConfig cluster = GenerateYarpClusterOrGetExistingOne(clusters, consulService);

            // If it's a new cluster, an empty Destination collection added, or if the cluster already exists, get its destinations collection
            var destinations = cluster.Destinations is null
                                    ? new Dictionary<string, DestinationConfig>()
                                    : new Dictionary<string, DestinationConfig>(cluster.Destinations);

            // service cluster destination full address
            var serviceAddressIncludingPort = $"{consulService.Address}:{consulService.Port}";

            destinations.Add(consulService.ID, new DestinationConfig
            {
                Address = serviceAddressIncludingPort
            });

            // append new destination with the previous one
            var newCluster = cluster with
            {
                Destinations = destinations
            };

            if (!await IsValidYarpClusterAsync(newCluster, consulService.Service))
            {
                continue;
            }

            clusters[consulService.Service] = newCluster;
        }

        return clusters.Values.ToList();
    }

    private async Task<bool> IsValidYarpClusterAsync(ClusterConfig cluster, string consulServiceName)
    {
        var clusterErrs = await _proxyConfigValidator.ValidateClusterAsync(cluster);

        if (clusterErrs.Any())
        {
            _logger.LogError("Errors found when trying to generate clusters for {Service}", consulServiceName);
            clusterErrs.ToList().ForEach(err => _logger.LogError(err, $"{consulServiceName} cluster validation error"));

            return false;
        }

        return true;
    }

    private static ClusterConfig GenerateYarpClusterOrGetExistingOne(Dictionary<string, ClusterConfig> existingClustersToLookup, AgentService consulService)
    {
        _ = consulService.Meta.TryGetValue("service_health_check_endpoint", out string? serviceHealthCheckEndpoint);
        _ = consulService.Meta.TryGetValue("service_health_check_seconds", out string? serviceHealthCheckSeconds);
        _ = consulService.Meta.TryGetValue("service_health_check_timeout_seconds", out string? serviceHealthTimeoutSeconds);

        var generatedClusterOrExistingOne = existingClustersToLookup.TryGetValue(consulService.Service, out var existingCluster)
                                        ? existingCluster
                                        : new ClusterConfig
                                        {
                                            ClusterId = GenerateClusterIdByServiceName(consulService.Service),
                                            LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,

                                            HealthCheck = new()
                                            {
                                                Active = new ActiveHealthCheckConfig
                                                {
                                                    Enabled = true,
                                                    Path = serviceHealthCheckEndpoint,
                                                    Interval = TimeSpan.FromSeconds(int.Parse(serviceHealthCheckSeconds)),
                                                    Timeout = TimeSpan.FromSeconds(int.Parse(serviceHealthTimeoutSeconds)),
                                                    Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
                                                }
                                            },

                                            Metadata = new Dictionary<string, string>
                                            {
                                                    {
                                                        ConsecutiveFailuresHealthPolicyOptions.ThresholdMetadataName, "5"
                                                    }
                                            }
                                        };

        return generatedClusterOrExistingOne;
    }

    private static string GenerateClusterIdByServiceName(string serviceName)
    {
        return $"{serviceName}-cluster";
    }
}
