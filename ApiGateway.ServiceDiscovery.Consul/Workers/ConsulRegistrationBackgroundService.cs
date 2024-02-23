using System.Net;
using System.Text.Json;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

internal class ConsulRegistrationBackgroundService : BackgroundService
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulRegistrationBackgroundService> _logger;
    private readonly IConfigurationSection _appRegistrationConfig;
    private readonly IConfigurationSection _appRegistrationConfigMeta;
    private AgentServiceRegistration? _serviceRegistration;

    public ConsulRegistrationBackgroundService(IConsulClient consulClient
                    , IConfiguration configuration
                    , ILogger<ConsulRegistrationBackgroundService> logger)
    {
        _consulClient = consulClient;
        _appRegistrationConfig = configuration.GetRequiredSection("ConsulServiceDiscovery:ServiceRegistration");
        _appRegistrationConfigMeta = configuration.GetRequiredSection("ConsulServiceDiscovery:ServiceRegistration:Meta");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Set config values
            PrepareServiceRegistration();

            // Set unique ID
            _serviceRegistration!.ID = GenerateUniqueIdForThisInstance();
            _logger.LogDebug("Service instance unique ID set as : `{ID}`", _serviceRegistration!.ID);
            
            _logger.LogDebug("First, deregister the same service[s] (same instance ID) `{ID}` if there is any...", _serviceRegistration!.ID);
            var deregisterPreviousInstancesOfThisServiceResult = await _consulClient.Agent.ServiceDeregister(_serviceRegistration.ID, stoppingToken);
            _logger.LogDebug("deregistering same service[s] with `{ID}` result {result}", _serviceRegistration!.ID, deregisterPreviousInstancesOfThisServiceResult.StatusCode);

            // Set hostname
            _serviceRegistration.Address = await FetchServiceAddressAsync(stoppingToken);
            _logger.LogInformation("Service address set as : `{Address}`", _serviceRegistration.Address);

            // Health Check
            _serviceRegistration.Checks = PrepareHealthChecks();
            var checksAsJson = JsonSerializer.Serialize(_serviceRegistration.Checks);
            _logger.LogInformation("Health checks set as : `{HealthChecks}`", checksAsJson);

            var result = await _consulClient.Agent.ServiceRegister(_serviceRegistration, stoppingToken);
            _logger.LogInformation("Service register statusCode : `{result}`", result.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $" Unable to register service");
        }
    }

    private AgentServiceCheck[] PrepareHealthChecks()
    {
        var serviceAddressUri = new Uri(_serviceRegistration!.Address);
        var serviceHost = serviceAddressUri.Host;
        var servicePort = _appRegistrationConfig.GetValue<int>("Port");

        var serviceHealthEndpoint = $"{_serviceRegistration.Address}:{servicePort}{_appRegistrationConfigMeta.GetValue<string>("service_health_endpoint")}";
        _logger.LogDebug("Service healthCheck : {healthCheck}", serviceHealthEndpoint);
        var serviceHealthCheckSeconds = _appRegistrationConfigMeta.GetValue<int>("service_health_check_seconds");
        var serviceHealthTimeoutSeconds = _appRegistrationConfigMeta.GetValue<int>("service_health_timeout_seconds");
        var serviceHealthDeregisterSeconds = _appRegistrationConfigMeta.GetValue<int>("service_health_deregister_seconds");

        var tcpCheck = new AgentServiceCheck
        {
            TCP = $"{serviceHost}:{servicePort}",
            Interval = TimeSpan.FromSeconds(serviceHealthCheckSeconds),
            Timeout = TimeSpan.FromSeconds(serviceHealthTimeoutSeconds),
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceHealthDeregisterSeconds),
        };

        var httpCheck = new AgentServiceCheck
        {
            HTTP = serviceHealthEndpoint,
            Interval = TimeSpan.FromSeconds(serviceHealthCheckSeconds),
            Timeout = TimeSpan.FromSeconds(serviceHealthTimeoutSeconds),
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceHealthDeregisterSeconds),
            TLSSkipVerify = true
        };

        return [tcpCheck, httpCheck];
    }

    private string GenerateUniqueIdForThisInstance()
    {
        var rand = new Random();
        var instanceId = rand.Next().ToString();

        return $"{_serviceRegistration.Name}-{_serviceRegistration.Port}";
    }

    private void PrepareServiceRegistration()
    {
        _serviceRegistration = new AgentServiceRegistration
        {
            Name = _appRegistrationConfig.GetValue<string>("Name"),
            Port = _appRegistrationConfig.GetValue<int>("Port"),
            Tags = _appRegistrationConfig.GetSection("Tags").Get<string[]>(),
            Meta = _appRegistrationConfig.GetSection("Meta").GetChildren()
                .ToDictionary(x => x.Key, x => x.Value)
        };
    }

    private async Task<string> FetchServiceAddressAsync(CancellationToken stoppingToken)
    {
        var appAddressInConfig = _appRegistrationConfig.GetValue<string>("Address");
        if (string.IsNullOrWhiteSpace(appAddressInConfig) == false)
            return appAddressInConfig;

        var dnsHostName = Dns.GetHostName();
        var hostname = await Dns.GetHostEntryAsync(dnsHostName, stoppingToken);
        //_serviceRegistration.Address = //$"http://192.168.0.104";

        return $"http://{hostname.HostName}";
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceRegistration != null)
        {
            _logger.LogInformation("Deregistering service");

            await _consulClient.Agent.ServiceDeregister(_serviceRegistration.ID,
                cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
