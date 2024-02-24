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

        public string ExportConfigs()
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
            _logger.LogInformation("Updating route configs (routes/clusters) from Consul...");

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
                    _logger.LogError("Updating proxy configs failed with status code: {statusCode} - {response}", serviceResult.StatusCode, serviceResult.Response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Updating proxy configs failed with exception : {error}", ex);
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
                if (IsRouteAlreadyAddedToCollection(routes, consulService))
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

        private static bool IsRouteAlreadyAddedToCollection(List<RouteConfig> routes, AgentService consulService)
        {
            return routes.Any(r => r.ClusterId == consulService.Service);
        }

        private static bool IsYarpEnabledForThisConsulService(AgentService consulService)
        {
            return consulService.Meta.TryGetValue("yarp_is_enabled", out string? isYarpEnabled)
                && isYarpEnabled.Equals("true", StringComparison.InvariantCulture);
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

        private static RouteConfig FetchSwaggerRouteDefinitionFromConsulService(AgentService svc)
        {
            return new RouteConfig
            {
                ClusterId = svc.Service,
                RouteId = $"{svc.Service}-swagger-route",

                Match = new RouteMatch
                {
                    Path = $"/swagger-json/{svc.Service}/swagger/v1/swagger.json"
                },

                Transforms = new IReadOnlyDictionary<string, string>[]
                {
                        new Dictionary<string, string>
                        {
                            ["PathRemovePrefix"] = $"/swagger-json/{svc.Service}"
                        }
                }
            };
        }

        private static RouteConfig FetchRouteDefinitionFromConsulService(AgentService svc)
        {
            return new RouteConfig
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
        }

        private async Task<List<ClusterConfig>> ExtractClusters(QueryResult<Dictionary<string, AgentService>> serviceResult)
        {
            var clusters = new Dictionary<string, ClusterConfig>();
            var serviceMapping = serviceResult.Response;

            foreach (var (key, consulService) in serviceMapping)
            {
                _ = consulService.Meta.TryGetValue("service_health_endpoint", out string? serviceHealthCheckEndpoint);
                _ = consulService.Meta.TryGetValue("service_health_check_seconds", out string? serviceHealthCheckSeconds);
                _ = consulService.Meta.TryGetValue("service_health_timeout_seconds", out string? serviceHealthTimeoutSeconds);

                ClusterConfig cluster = GenerateYarpClusterOrGetExistingOne(clusters, consulService);

                // If it's a new cluster, an empty Destination collection added, or if the cluster already exists, get its destinations collection
                var destination = cluster.Destinations is null
                                        ? new Dictionary<string, DestinationConfig>()
                                        : new Dictionary<string, DestinationConfig>(cluster.Destinations);

                // service cluster destination full address
                var address = $"{consulService.Address}:{consulService.Port}";

                destination.Add(consulService.ID, new DestinationConfig { Address = address, Health = address });

                // append new destination with the previous one
                var newCluster = cluster with
                {
                    Destinations = destination
                };

                if (!await IsValidYarpClusterAsync(newCluster, consulService.Service))
                {
                    continue;
                }

                var clusterErrs = await _proxyConfigValidator.ValidateClusterAsync(newCluster);
                if (clusterErrs.Any())
                {
                    _logger.LogError("Errors found when creating clusters for {Service}", consulService.Service);
                    clusterErrs.ToList().ForEach(err => _logger.LogError(err, $"{consulService.Service} cluster validation error"));

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

        private static ClusterConfig GenerateYarpClusterOrGetExistingOne(Dictionary<string, ClusterConfig> clusters, AgentService service)
        {
            var generatedClusterOrExistingOne = clusters.TryGetValue(service.Service, out var existingCluster)
                                            ? existingCluster
                                            : new ClusterConfig
                                            {
                                                ClusterId = service.Service,
                                                LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                                                ////////HealthCheck = new()
                                                ////////{
                                                ////////    Active = new ActiveHealthCheckConfig
                                                ////////    {
                                                ////////        Enabled = true,
                                                ////////        Interval = TimeSpan.FromSeconds(int.Parse(serviceHealthCheckSeconds)),
                                                ////////        Timeout = TimeSpan.FromSeconds(int.Parse(serviceHealthTimeoutSeconds)),
                                                ////////        Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
                                                ////////        Path = serviceHealthCheckEndpoint
                                                ////////    }
                                                ////////},

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
