﻿using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Workers;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ReverseProxyExtensions
{
    public static IReverseProxyBuilder LoadFromConsul(this IReverseProxyBuilder builder, IConfiguration configuration)
    {
        builder.LoadFromMemory(default, default);

        var isAutoReloadEnabled = configuration.GetValue<bool?>("ConsulServiceDiscovery:AutoDiscovery:IsEnabled");
        if (isAutoReloadEnabled is null)
        {
            throw new ArgumentException("Invalid configurations. `ConsulServiceDiscovery:AutoDiscovery:IsEnabled` not found in the configuration");
        }

        if (isAutoReloadEnabled == true)
        {
            builder.Services.AddHostedService<ConsulMonitorBackgroundService>();
        }

        return builder;
    }
}