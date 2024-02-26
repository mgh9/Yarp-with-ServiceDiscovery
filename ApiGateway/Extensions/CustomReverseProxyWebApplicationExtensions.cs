using System.Text.Json;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomReverseProxyWebApplicationExtensions
{
    internal static void UseCustomReverseProxy(this WebApplication app)
    {
        LogImportantConfigs(app);

        app.MapReverseProxy();
    }

    private static void LogImportantConfigs(WebApplication app)
    {
        var consulClientOptions = app.Configuration.GetSection("ConsulServiceDiscovery:ConsulClient").Get<ConsulClientOptions>();
        app.Logger.LogInformation("ConsulServiceDiscovery ConsulClient options: {config}", JsonSerializer.Serialize(consulClientOptions));

        var isAutoDiscoveryEnabled = app.Configuration.GetValue<bool>("ConsulServiceDiscovery:AutoDiscovery:IsEnabled");
        if (isAutoDiscoveryEnabled)
        {
            app.Logger.LogInformation("AutoDiscovery is enabled");
        }
        else
        {
            app.Logger.LogWarning("AutoDiscovery is disabled");
        }
    }
}
