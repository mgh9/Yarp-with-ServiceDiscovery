using System.Net;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Consul;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.LoadBalancing;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

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
            ArgumentNullException.ThrowIfNull(consulClient);
            ArgumentNullException.ThrowIfNull(proxyConfigProvider);
            ArgumentNullException.ThrowIfNull(proxyConfigValidator);
            ArgumentNullException.ThrowIfNull(logger);

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

        public string ExportRoutesAndClustersAsJson()
        {
            var config = new
            {
                Routes = GetRoutes(),
                Clusters = GetClusters()
            };

            return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public async Task ReloadRoutesAndClustersAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reloading Routes and Clusters from Consul ServiceRegistry...");

            try
            {
                var serviceResult = await _consulClient.Agent.Services(cancellationToken);
                if (serviceResult.StatusCode == HttpStatusCode.OK)
                {
                    var routes = await ExtractRoutes(serviceResult);
                    var clusters = await ExtractClusters(serviceResult);

                    _proxyConfigProvider.Update(routes, clusters);

                    _logger.LogInformation("Proxy configs (routes/clusters) reloaded");
                    _logger.LogInformation("New Routes count: {routesCount} and new Clusters count:{clustersCount}", routes.Count, clusters.Count);
                }
                else
                {
                    _logger.LogCritical("Updating Routes and Clusters from Consul ServiceRegistry failed with the status code `{statusCode}` and response `{response}`"
                        , serviceResult.StatusCode, serviceResult.Response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Updating Routes and Clusters failed with the exception : {error}", ex);
                throw;
            }
        }

        private async Task<List<RouteConfig>> ExtractRoutes(QueryResult<Dictionary<string, AgentService>> serviceResult)
        {
            var serviceMapping = serviceResult.Response;
            var routes = new List<RouteConfig>();

            foreach (var (key, consulService) in serviceMapping)
            {
                if (!IsYarpEnabledForThisConsulService(consulService))
                    continue;

                // ignore duplicate service type - route
                if (IsRouteAlreadyAddedToCollection(routes, consulService.Service))
                    continue;

                var mainRoute = FetchRouteDefinitionFromConsulService(consulService);
                if (!await IsValidYarpRouteAsync(mainRoute, consulService.Service))
                {
                    continue;
                }

                var swaggerRoute = FetchSwaggerRouteDefinitionFromConsulService(consulService);
                if (!await IsValidYarpRouteAsync(swaggerRoute, consulService.Service))
                {
                    continue;
                }

                routes.Add(mainRoute);
                routes.Add(swaggerRoute);
            }

            return routes;
        }

        private static bool IsRouteAlreadyAddedToCollection(List<RouteConfig> routes, string serviceName)
        {
            //return routes.Any(r => r.ClusterId == consulService.Service);
            var preparedRouteId = GenerateRouteIdByServiceName(serviceName);
            return routes.Any(r => r.RouteId == preparedRouteId);
        }

        private static bool IsYarpEnabledForThisConsulService(AgentService consulService)
        {
            return consulService.Meta.TryGetValue("yarp_is_enabled", out string? isYarpEnabled)
                && isYarpEnabled.Equals("true", StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<bool> IsValidYarpRouteAsync(RouteConfig route, string consulServiceName)
        {
            var routeErrs = await _proxyConfigValidator.ValidateRouteAsync(route);

            if (routeErrs.Any())
            {
                _logger.LogError("Errors found when trying to generate routes for {Service}", consulServiceName);
                routeErrs.ToList().ForEach(err => _logger.LogError(err, $"{consulServiceName} route validation error"));

                return false;
            }

            return true;
        }

        private static string GenerateRouteIdByServiceName(string serviceName)
        {
            return $"{serviceName}-route";
        }

        private static string GenerateSwaggerRouteIdByServiceName(string serviceName)
        {
            return $"{serviceName}-swagger-route";
        }

        private static string GenerateClusterIdByServiceName(string serviceName)
        {
            return $"{serviceName}-cluster";
        }

        private static RouteConfig FetchSwaggerRouteDefinitionFromConsulService(AgentService consulService)
        {
            return new RouteConfig
            {
                RouteId = GenerateSwaggerRouteIdByServiceName(consulService.Service),
                ClusterId = GenerateClusterIdByServiceName(consulService.Service),

                Match = new RouteMatch
                {
                    Path = $"/swagger-json/{consulService.Service}/swagger/v1/swagger.json"
                },

                Transforms = new IReadOnlyDictionary<string, string>[]
                {
                        new Dictionary<string, string>
                        {
                            ["PathRemovePrefix"] = $"/swagger-json/{consulService.Service}"
                        }
                }
            };
        }

        private static RouteConfig FetchRouteDefinitionFromConsulService(AgentService consulService)
        {
            return new RouteConfig
            {
                RouteId = GenerateRouteIdByServiceName(consulService.Service),
                ClusterId =  GenerateClusterIdByServiceName(consulService.Service),

                Match = new RouteMatch
                {
                    Path = consulService.Meta.TryGetValue("yarp_route_match_path", out var yarpRouteMatchPath)
                                                    ? yarpRouteMatchPath
                                                    : string.Empty
                },

                Transforms = new IReadOnlyDictionary<string, string>[]
                                {
                                    new Dictionary<string, string>
                                    {
                                        ["PathPattern"] = consulService.Meta.TryGetValue("yarp_route_transform_path", out var yarpRouteTransformPath)
                                                ? yarpRouteTransformPath
                                                : string.Empty
                                    }
                                }
            };
        }

        private async Task<List<ClusterConfig>> ExtractClusters(QueryResult<Dictionary<string, AgentService>> serviceResult)
        {
            var clusters = new Dictionary<string, ClusterConfig>();
            var serviceMapping = serviceResult.Response;

            foreach (var (key, consulService) in serviceMapping)
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
                    Address = serviceAddressIncludingPort, 
                    //Health =  //serviceAddressIncludingPort
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

        private static ClusterConfig GenerateYarpClusterOrGetExistingOne(Dictionary<string, ClusterConfig> clusters, AgentService consulService)
        {
            _ = consulService.Meta.TryGetValue("service_health_check_endpoint", out string? serviceHealthCheckEndpoint);
            _ = consulService.Meta.TryGetValue("service_health_check_seconds", out string? serviceHealthCheckSeconds);
            _ = consulService.Meta.TryGetValue("service_health_check_timeout_seconds", out string? serviceHealthTimeoutSeconds);

            var generatedClusterOrExistingOne = clusters.TryGetValue(consulService.Service, out var existingCluster)
                                            ? existingCluster
                                            : new ClusterConfig
                                            {
                                                ClusterId = GenerateClusterIdByServiceName(consulService.Service),
                                                LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                                               
                                                ////HealthCheck = new()
                                                ////{
                                                ////    Active = new ActiveHealthCheckConfig
                                                ////    {
                                                ////        Enabled = true,
                                                ////        Path = serviceHealthCheckEndpoint,
                                                ////        Interval = TimeSpan.FromSeconds(int.Parse(serviceHealthCheckSeconds)),
                                                ////        Timeout = TimeSpan.FromSeconds(int.Parse(serviceHealthTimeoutSeconds)),
                                                ////        Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
                                                ////    }
                                                ////},

                                                Metadata = new Dictionary<string, string>
                                                {
                                                    {
                                                        ConsecutiveFailuresHealthPolicyOptions.ThresholdMetadataName, "5"
                                                    }
                                                }
                                            };

            return generatedClusterOrExistingOne;
        }
    }
}
