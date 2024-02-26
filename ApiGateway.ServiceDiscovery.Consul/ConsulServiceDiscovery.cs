using System.Net;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Consul;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Yarp.ReverseProxy.Configuration;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

namespace ApiGateway.ServiceDiscovery.Consul;

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
            Routes = GetRoutes() ?? [],
            Clusters = GetClusters() ?? []
        };

        var exported = JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        return exported;
    }

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reloading Routes and Clusters from Consul ServiceRegistry...");

        try
        {
            var getConsulAvailableServicesResult = await _consulClient.Agent.Services(cancellationToken);
            if (getConsulAvailableServicesResult.StatusCode == HttpStatusCode.OK)
            {
                var routes = await ExtractRoutes(getConsulAvailableServicesResult);
                var clusters = await ExtractClusters(getConsulAvailableServicesResult);

                _proxyConfigProvider.Update(routes, clusters);

                _logger.LogInformation("Proxy configs (routes/clusters) reloaded");
                _logger.LogInformation("New Routes count: {routesCount} and new Clusters count: {clustersCount}", routes.Count, clusters.Count);
            }
            else
            {
                _logger.LogCritical("Updating Routes and Clusters from Consul ServiceRegistry failed with the status code `{statusCode}` and response `{response}`"
                    , getConsulAvailableServicesResult.StatusCode, getConsulAvailableServicesResult.Response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Updating Routes and Clusters failed with the exception : {error}", ex);
            //throw;
        }
    }
}
