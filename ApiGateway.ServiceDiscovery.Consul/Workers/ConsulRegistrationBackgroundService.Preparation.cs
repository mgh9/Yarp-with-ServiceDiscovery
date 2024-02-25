using System.Net;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions.Exceptions;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

internal partial class ConsulRegistrationBackgroundService : BackgroundService
{
    private AgentServiceCheck[] PrepareHealthChecksInfoForRegistration()
    {
        try
        {
            var serviceAddressUri = new Uri(_consulServiceRegistration!.Address);
            var serviceHost = serviceAddressUri.Host;
            var servicePort = _appRegistrationConfig.GetValue<int>("Port");

            var serviceHealthEndpoint = $"{_consulServiceRegistration.Address}:{servicePort}{_appRegistrationConfigMeta.GetValue<string>("service_health_check_endpoint")}";
            _logger.LogDebug("Service healthCheck : {healthCheck}", serviceHealthEndpoint);

            var serviceHealthCheckSeconds = _appRegistrationConfigMeta.GetValue<int>("service_health_check_seconds");
            var serviceHealthTimeoutSeconds = _appRegistrationConfigMeta.GetValue<int>("service_health_check_timeout_seconds");
            var serviceHealthDeregisterSeconds = _appRegistrationConfigMeta.GetValue<int>("service_health_check_deregister_seconds");

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
        catch (Exception ex)
        {
            throw new InvalidServiceHealthCheckInfoException("Invalid health check configs in appSettings to register this service with ServiceRegistry", ex);
        }
    }

    private string GenerateUniqueIdForThisInstanceByRandomId()
    {
        var rand = new Random();
        var instanceId = rand.Next().ToString();

        return $"{_consulServiceRegistration!.Name}-{instanceId}";
    }

    private string GenerateUniqueIdForThisInstanceByNameAndPort()
    {
        return $"{_consulServiceRegistration!.Name}-{_consulServiceRegistration.Port}";
    }

    private void PrepareMainServiceInfoForRegistration()
    {
        try
        {
            _consulServiceRegistration = new AgentServiceRegistration
            {
                Name = _appRegistrationConfig.GetValue<string>("Name"),
                Port = _appRegistrationConfig.GetValue<int>("Port"),
                Tags = _appRegistrationConfig.GetSection("Tags").Get<string[]>(),
                Meta = _appRegistrationConfig.GetSection("Meta").GetChildren().ToDictionary(x => x.Key, x => x.Value)
            };
        }
        catch (Exception ex)
        {
            throw new InvalidServiceRegistrationInfoException("Invalid AppSettings configs (Name, Port, Tags, Meta) for registering in ServiceRegistry", ex);
        }
    }

    private async Task<string> FetchServiceAddressAsync(CancellationToken stoppingToken)
    {
        var appAddressInConfig = _appRegistrationConfig.GetValue<string>("Address");
        _logger.LogDebug("Trying to fetch service address from appSettings in `Address` key...");
        if (string.IsNullOrWhiteSpace(appAddressInConfig) == false)
        {
            _logger.LogDebug("Service address from appSettings in `Address` key is : `{address}`", appAddressInConfig);
            return appAddressInConfig;
        }

        _logger.LogDebug("Service address not found in appSettings `Address` key, trying to fetch from localhost dns name...");
        var dnsHostName = Dns.GetHostName();
        var hostname = await Dns.GetHostEntryAsync(dnsHostName, stoppingToken);
        _logger.LogDebug("Service hostName is `{hostName}`", hostname);

        var serviceAddress = $"http://{hostname.HostName}";
        return serviceAddress;
    }
}
