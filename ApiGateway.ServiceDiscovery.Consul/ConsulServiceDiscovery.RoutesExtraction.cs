using Consul;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Yarp.ServiceDiscovery.Abstractions;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

namespace Yarp.ServiceDiscovery.Consul;

public partial class ConsulServiceDiscovery : IServiceDiscovery
{
    private async Task<List<RouteConfig>> ExtractRoutes(QueryResult<Dictionary<string, AgentService>> getServicesResult)
    {
        var routes = new List<RouteConfig>();
        var serviceNameToItsDataMapping = getServicesResult.Response;

        foreach (var (serviceName, consulService) in serviceNameToItsDataMapping)
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
            ClusterId = GenerateClusterIdByServiceName(consulService.Service),

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
}
