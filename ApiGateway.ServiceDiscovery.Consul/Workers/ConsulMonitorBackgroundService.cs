using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

public class ConsulMonitorBackgroundService : BackgroundService
{
    public const int DEFAULT_CONSUL_POLL_INTERVAL_SECONDS = 3000;

    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<ConsulMonitorBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _reloadIntervalInSeconds;

    public ConsulMonitorBackgroundService(IServiceDiscovery serviceDiscovery, ILogger<ConsulMonitorBackgroundService> logger
        , IConfiguration configuration)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
        _configuration = configuration;

        _reloadIntervalInSeconds = _configuration.GetValue<int>("ConsulServiceDiscovery:ReloadRoutesAndClusters:IntervalSeconds");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Start updating configs (routes/clusters) from Consul ServiceDiscovery...");
            await _serviceDiscovery.ReloadRoutesAndClustersAsync(stoppingToken);
            _logger.LogInformation("Route configs (routes/clusters) from Consul ServiceDiscovery reloaded.");

            _logger.LogInformation("Next reloading in {PollSeconds} seconds", _reloadIntervalInSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_reloadIntervalInSeconds), stoppingToken);
        }
    }
}
