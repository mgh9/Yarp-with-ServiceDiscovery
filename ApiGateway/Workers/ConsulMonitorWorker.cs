using ApiGateway.ServiceDiscovery.Abstractions;

namespace ApiGateway.Workers
{
    public class ConsulMonitorWorker : BackgroundService
    {
        private const int DEFAULT_CONSUL_POLL_INTERVAL_MINS = 2;
        private const int DEFAULT_CONSUL_POLL_INTERVAL_SECONDS = 30;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly ILogger<ConsulMonitorWorker> _logger;

        public ConsulMonitorWorker(IServiceDiscovery serviceDiscovery, ILogger<ConsulMonitorWorker> logger)
        {
            _serviceDiscovery = serviceDiscovery;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Start updating route configs (routes/clusters) from ServiceDiscovery...");

                await _serviceDiscovery.ReloadRoutesAndClustersAsync(stoppingToken);

                _logger.LogInformation("Route configs (routes/clusters) from ServiceDiscovery reloaded.");

                _logger.LogDebug("Delay for next reloading in {PollSeconds} seconds", DEFAULT_CONSUL_POLL_INTERVAL_SECONDS);
                await Task.Delay(TimeSpan.FromSeconds(DEFAULT_CONSUL_POLL_INTERVAL_SECONDS), stoppingToken);
            }
        }
    }
}
