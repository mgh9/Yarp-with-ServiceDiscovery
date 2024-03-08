using System.Net;
using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ServiceDiscovery.Abstractions.Exceptions;
using Yarp.ServiceDiscovery.Consul.Options;

namespace Yarp.ServiceDiscovery.Consul.Workers;

internal partial class ConsulRegistrationBackgroundService : BackgroundService
{
    private AgentServiceCheck[] PrepareHealthChecksInfoForRegistration()
    {
        try
        {
            var serviceAddressUri = new Uri(_consulServiceRegistration!.Address);
            var serviceHost = serviceAddressUri.Host;

            var serviceHealthEndpointAbsoluteUrl = $"{_consulServiceRegistration.Address}:{_consulServiceRegistrationOptions.Port}{_consulServiceRegistrationOptions.ServiceHealthCheckEndpoint}";
            _logger.LogInformation("Service http[s] healthCheck address to register in ServiceRegistry : `{healthCheck}`", serviceHealthEndpointAbsoluteUrl);

            var tcpCheck = new AgentServiceCheck
            {
                TCP = $"{serviceHost}:{_consulServiceRegistrationOptions.Port}",
                Interval = TimeSpan.FromSeconds(_consulServiceRegistrationOptions.ServiceHealthCheckSeconds),
                Timeout = TimeSpan.FromSeconds(_consulServiceRegistrationOptions.ServiceHealthCheckTimeoutSeconds),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(_consulServiceRegistrationOptions.ServiceHealthCheckDeregisterSeconds),
            };

            var httpCheck = new AgentServiceCheck
            {
                HTTP = serviceHealthEndpointAbsoluteUrl,
                Interval = TimeSpan.FromSeconds(_consulServiceRegistrationOptions.ServiceHealthCheckSeconds),
                Timeout = TimeSpan.FromSeconds(_consulServiceRegistrationOptions.ServiceHealthCheckTimeoutSeconds),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(_consulServiceRegistrationOptions.ServiceHealthCheckDeregisterSeconds),
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
                Name = _consulServiceRegistrationOptions.Name,
                Port = _consulServiceRegistrationOptions.Port,
                Tags = _consulServiceRegistrationOptions.Tags,
                Meta = _consulServiceRegistrationOptions.Meta
            };
        }
        catch (Exception ex)
        {
            throw new InvalidServiceRegistrationInfoException("Invalid AppSettings configs (Name, Port, Tags, Meta) for registering in ServiceRegistry", ex);
        }
    }

    private async Task<string> FetchServiceAddressAsync(CancellationToken stoppingToken)
    {
        if (TryToFetchServiceAddressFromAppSettings(out string? serviceAddress))
        {
            _logger.LogDebug("Service address found in appSettings : `{address}`", serviceAddress);
            return serviceAddress!;
        }

        _logger.LogDebug("Service address not found in appSettings `Address` key, trying to fetch from localhost dns name...");
        serviceAddress = await FetchServiceAddressFromDnsAsync(stoppingToken);
        return serviceAddress!;
    }

    private async Task<string> FetchServiceAddressFromDnsAsync(CancellationToken stoppingToken)
    {
        var dnsHostName = Dns.GetHostName();
        var hostname = await Dns.GetHostEntryAsync(dnsHostName, stoppingToken);
        var hostnameValue = hostname.HostName?.ToString()?.ToLowerInvariant();

        _logger.LogDebug("Service hostName is `{hostName}`", hostnameValue);
        if (hostnameValue is null)
        {
            throw new InvalidServiceRegistrationInfoException("Cannot fetch hostname from HostEntry of the machine");
        }

        var serviceAddress = _consulServiceRegistrationOptions.Scheme ?? "http";
        serviceAddress += $"://{hostnameValue}";

        return serviceAddress;
    }

    private bool TryToFetchServiceAddressFromAppSettings(out string? serviceAddress)
    {
        _logger.LogDebug("Trying to fetch service address from appSettings in `Address` key...");

        serviceAddress = _consulServiceRegistrationOptions.Address;
        if (string.IsNullOrWhiteSpace(_consulServiceRegistrationOptions.Address) == false)
        {
            var scheme = _consulServiceRegistrationOptions.Scheme ?? "http";
            serviceAddress = $"{scheme}://{_consulServiceRegistrationOptions.Address}";

            return true;
        }

        return false;
    }

    private static void PrepareServiceRegistrationOptions(ServiceInfoOptions options)
    {
        options.Scheme ??= "http";

        // health check endpoint
        options.ServiceHealthCheckEndpoint = options.Meta?.GetValueOrDefault("service_health_check_endpoint");
        if (options.ServiceHealthCheckEndpoint?.StartsWith('/') == false)
        {
            options.Meta!["service_health_check_endpoint"] = "/" + options.Meta["service_health_check_endpoint"];
            options.ServiceHealthCheckEndpoint = "/" + options.ServiceHealthCheckEndpoint;
        }

        var tmp2 = options.Meta?.GetValueOrDefault("yarp_is_enabled");
        options.YarpIsEnabled = tmp2?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true;

        options.YarpRouteMatchPath = options.Meta?.GetValueOrDefault("yarp_route_match_path");
        options.YarpRouteTransformPath = options.Meta?.GetValueOrDefault("yarp_route_transform_path");

        if (options.Meta?.TryGetValue("service_health_check_seconds", out string? tmp3) == true)
        {
            options.ServiceHealthCheckSeconds = int.Parse(tmp3);
        }

        if (options.Meta?.TryGetValue("service_health_check_timeout_seconds", out string? tmp4) == true)
        {
            options.ServiceHealthCheckTimeoutSeconds = int.Parse(tmp4);
        }

        if (options.Meta?.TryGetValue("service_health_check_deregister_seconds", out string? tmp5) == true)
        {
            options.ServiceHealthCheckDeregisterSeconds = int.Parse(tmp5);
        }
    }
}
