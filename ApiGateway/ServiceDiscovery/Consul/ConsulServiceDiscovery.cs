using Consul;
using System.Net;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.LoadBalancing;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using ApiGateway.ServiceDiscovery.Abstractions;

namespace ApiGateway.ServiceDiscovery.Consul
{
    public partial class ConsulServiceDiscovery : IServiceDiscovery
    {
        private readonly ILogger<ConsulServiceDiscovery> _logger;

        private readonly IConsulClient _consulClient;
        private readonly IConfigValidator _proxyConfigValidator;
        private readonly InMemoryConfigProvider _proxyConfigProvider;

        public ConsulServiceDiscovery(IConsulClient consulClient
                                    , IConfigValidator proxyConfigValidator
                                    , InMemoryConfigProvider proxyConfigProvider
                                    , ILogger<ConsulServiceDiscovery> logger)
        {
            _consulClient = consulClient;
            _proxyConfigValidator = proxyConfigValidator;
            _proxyConfigProvider = proxyConfigProvider;
            _logger = logger;
        }

        public IReadOnlyList<ClusterConfig> GetClusters()
        {
            return _proxyConfigProvider.GetConfig().Clusters;
        }

        public IReadOnlyList<RouteConfig> GetRoutes()
        {
            return _proxyConfigProvider.GetConfig().Routes;
        }

        public async Task ReloadRoutesAndClustersAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Updating route configs (routes/clusters) from Consul...");
            var serviceResult = await _consulClient.Agent.Services(stoppingToken);

            if (serviceResult.StatusCode == HttpStatusCode.OK)
            {
                var routes = await ExtractRoutes(serviceResult);
                var clusters = await ExtractClusters(serviceResult);

                _proxyConfigProvider.Update(routes, clusters);
            }

            _logger.LogInformation("Proxy configs (routes/clusters) reloaded");
        }

        private async Task<List<RouteConfig>> ExtractRoutes(QueryResult<Dictionary<string, AgentService>> serviceResult)
        {
            var serviceMapping = serviceResult.Response;
            var routes = new List<RouteConfig>();

            foreach (var (key, svc) in serviceMapping)
            {
                if (!svc.Meta.TryGetValue("yarp_is_enabled", out string? isYarpEnabled) ||
                    !isYarpEnabled.Equals("true", StringComparison.InvariantCulture))
                    continue;

                // ignore duplicate service type
                if (routes.Any(r => r.ClusterId == svc.Service))
                    continue;

                var route = new RouteConfig
                {
                    ClusterId = svc.Service,
                    RouteId = $"{svc.Service}-route",

                    Match = new RouteMatch
                    {
                        Path = svc.Meta.TryGetValue("yarp_route_match_path", out var yarpRouteMatchPath)
                            ? yarpRouteMatchPath
                            : string.Empty
                    },

                    Transforms = new IReadOnlyDictionary<string, string>[]
                    {
                        new Dictionary<string, string>
                        {
                            ["PathPattern"] = svc.Meta.TryGetValue("yarp_route_transform_path", out var yarpRouteTransformPath)
                                    ? yarpRouteTransformPath
                                    : string.Empty
                        }
                    }
                };

                var routeErrs = await _proxyConfigValidator.ValidateRouteAsync(route);
                if (routeErrs.Any())
                {
                    _logger.LogError("Errors found when trying to generate routes for {Service}",
                        svc.Service);
                    routeErrs.ToList().ForEach(err =>
                        _logger.LogError(err, $"{svc.Service} route validation error"));
                    continue;
                }

                routes.Add(route);
            }

            return routes;
        }

        private async Task<List<ClusterConfig>> ExtractClusters(QueryResult<Dictionary<string, AgentService>> serviceResult)
        {
            // TODO: add validation

            var clusters = new Dictionary<string, ClusterConfig>();
            var serviceMapping = serviceResult.Response;

            foreach (var (key, svc) in serviceMapping)
            {
                _ = svc.Meta.TryGetValue("service_health_endpoint", out string? serviceHealthCheckEndpoint);
                _ = svc.Meta.TryGetValue("service_health_check_seconds", out string? serviceHealthCheckSeconds);
                _ = svc.Meta.TryGetValue("service_health_timeout_seconds", out string? serviceHealthTimeoutSeconds);

                var cluster = clusters.TryGetValue(svc.Service, out var existingCluster)
                    ? existingCluster
                    : new ClusterConfig
                    {
                        ClusterId = svc.Service,
                        LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                        //HealthCheck = new()
                        //{
                        //    Active = new ActiveHealthCheckConfig
                        //    {
                        //        Enabled = true,
                        //        Interval = TimeSpan.FromSeconds(int.Parse(serviceHealthCheckSeconds)),
                        //        Timeout = TimeSpan.FromSeconds(int.Parse(serviceHealthTimeoutSeconds)),
                        //        Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
                        //        Path = serviceHealthCheckEndpoint
                        //    }
                        //},

                        Metadata = new Dictionary<string, string>
                        {
                            {
                                ConsecutiveFailuresHealthPolicyOptions.ThresholdMetadataName, "5"
                            }
                        }
                    };

                var destination = cluster.Destinations is null
                    ? new Dictionary<string, DestinationConfig>()
                    : new Dictionary<string, DestinationConfig>(cluster.Destinations);

                var address = $"{svc.Address}:{svc.Port}";

                destination.Add(svc.ID, new DestinationConfig { Address = address, Health = address });

                var newCluster = cluster with
                {
                    Destinations = destination
                };

                var clusterErrs = await _proxyConfigValidator.ValidateClusterAsync(newCluster);
                if (clusterErrs.Any())
                {
                    _logger.LogError("Errors found when creating clusters for {Service}", svc.Service);
                    clusterErrs.ToList().ForEach(err => _logger.LogError(err, $"{svc.Service} cluster validation error"));

                    continue;
                }

                clusters[svc.Service] = newCluster;
            }

            return clusters.Values.ToList();
        }
    }
}
