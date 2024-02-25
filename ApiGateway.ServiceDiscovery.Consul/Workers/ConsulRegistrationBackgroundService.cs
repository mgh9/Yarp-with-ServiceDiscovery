using System.Net;
using System.Text.Json;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions.Exceptions;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;

internal partial class ConsulRegistrationBackgroundService : BackgroundService
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulRegistrationBackgroundService> _logger;
    private readonly IConfigurationSection _appRegistrationConfig;
    private readonly IConfigurationSection _appRegistrationConfigMeta;

    private AgentServiceRegistration? _consulServiceRegistration;

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
            await TryRegisterServiceAsync(stoppingToken);
        }
        catch (InvalidServiceRegistrationInfoException iex)
        {
            _logger.LogCritical(iex, $"Unable to register service in ServiceRegistry, because of invalid AppSettings configs");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Unable to register service in ServiceRegistry");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Consul registration background service is stopping...");
        if (_consulServiceRegistration is not null)
        {
            _logger.LogInformation("Deregistering the service ID `{serviceId}`...", _consulServiceRegistration.ID);
            var deregisteringResult = await _consulClient.Agent.ServiceDeregister(_consulServiceRegistration.ID, cancellationToken);
            _logger.LogInformation("Deregistering the service ID `{serviceId}` result code: `{result}`", _consulServiceRegistration.ID, deregisteringResult.StatusCode);
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task TryRegisterServiceAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Registering service with ServiceRegistry...");

        // Fetch config values
        PrepareMainServiceInfoForRegistration();

        // Set unique ID
        _consulServiceRegistration!.ID = GenerateUniqueIdForThisInstanceByNameAndPort();
        _logger.LogDebug("Service instance unique ID generated as : `{ID}`", _consulServiceRegistration!.ID);

        _logger.LogDebug("First, deregister the same service[s] (same instance ID) with ID: `{ID}` from ServiceRegistry if there is any...", _consulServiceRegistration!.ID);
        var deregisterPreviousInstancesOfThisServiceResult = await _consulClient.Agent.ServiceDeregister(_consulServiceRegistration.ID, stoppingToken);
        _logger.LogDebug("Deregistering the same service[s] with `{ID}` from ServiceRegistry result status code: `{result}`", _consulServiceRegistration!.ID, deregisterPreviousInstancesOfThisServiceResult.StatusCode);
        if (deregisterPreviousInstancesOfThisServiceResult.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogWarning("Deregistering the same service[s] with `{ID}` from ServiceRegistry " +
                "result WAS NOT successful (maybe the service didn't registered already. result status code: `{result}`", _consulServiceRegistration!.ID, deregisterPreviousInstancesOfThisServiceResult.StatusCode);
        }

        // Set hostname
        _consulServiceRegistration.Address = await FetchServiceAddressAsync(stoppingToken);
        _logger.LogInformation("Service address : `{Address}`", _consulServiceRegistration.Address);

        // Health Check
        _consulServiceRegistration.Checks = PrepareHealthChecksInfoForRegistration();
        var checksAsJson = JsonSerializer.Serialize(_consulServiceRegistration.Checks);
        _logger.LogInformation("Health checks : `{HealthChecks}`", checksAsJson);

        var serviceRegistrationResult = await _consulClient.Agent.ServiceRegister(_consulServiceRegistration, stoppingToken);
        _logger.LogInformation("Service register statusCode : `{result}`", serviceRegistrationResult.StatusCode.ToString());
        if (serviceRegistrationResult.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogCritical("Registering the service with `{ID}` in ServiceRegistry failed with the result: `{result}`", _consulServiceRegistration!.ID, serviceRegistrationResult.StatusCode);
        }
    }
}
