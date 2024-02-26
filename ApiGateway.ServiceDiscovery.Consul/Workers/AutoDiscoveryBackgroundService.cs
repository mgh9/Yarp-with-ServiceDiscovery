using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

public class AutoDiscoveryBackgroundService : BackgroundService
{
    public const int DEFAULT_CONSUL_POLL_INTERVAL_SECONDS = 30;

    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<AutoDiscoveryBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _autoReloadIntervalSeconds;

    public AutoDiscoveryBackgroundService(IServiceDiscovery serviceDiscovery, ILogger<AutoDiscoveryBackgroundService> logger
        , IConfiguration configuration)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
        _configuration = configuration;

        _autoReloadIntervalSeconds = _configuration.GetValue<int?>("ConsulServiceDiscovery:AutoDiscovery:IntervalSeconds")
            ?? DEFAULT_CONSUL_POLL_INTERVAL_SECONDS;

        logger.LogDebug("AutoDiscovery (reloading Routes and Clusters automatically) interval set as `{interval}` seconds", _autoReloadIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Start reloading configs (routes/clusters) from Consul ServiceRegistry...");
            await _serviceDiscovery.ReloadAsync(stoppingToken);
            _logger.LogInformation("Route configs (routes/clusters) from Consul ServiceDiscovery reloaded.");

            _logger.LogInformation("Next reloading in {PollSeconds} seconds...", _autoReloadIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_autoReloadIntervalSeconds), stoppingToken);
        }
    }
}
