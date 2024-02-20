using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

public class ConsulMonitorBackgroundService : BackgroundService
{
    public const int DEFAULT_CONSUL_POLL_INTERVAL_SECONDS = 3000;

    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<ConsulMonitorBackgroundService> _logger;

    public ConsulMonitorBackgroundService(IServiceDiscovery serviceDiscovery, ILogger<ConsulMonitorBackgroundService> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Start updating configs (routes/clusters) from Consul ServiceDiscovery...");
            await _serviceDiscovery.ReloadRoutesAndClustersAsync(stoppingToken);
            _logger.LogInformation("Route configs (routes/clusters) from Consul ServiceDiscovery reloaded.");

            _logger.LogDebug("Next reloading in {PollSeconds} seconds", DEFAULT_CONSUL_POLL_INTERVAL_SECONDS);
            await Task.Delay(TimeSpan.FromSeconds(DEFAULT_CONSUL_POLL_INTERVAL_SECONDS), stoppingToken);
        }
    }
}
