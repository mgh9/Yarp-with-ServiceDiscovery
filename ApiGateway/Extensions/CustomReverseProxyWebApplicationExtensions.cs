using System.Text.Json;
using Newtonsoft.Json;

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
        var serviceDiscoveryConfigs = app.Configuration.GetSection("ConsulServiceDiscovery").Get<Dictionary<string,dynamic>>();
        app.Logger.LogInformation("ConsulServiceDiscovery configs: {config}", JsonConvert.SerializeObject(serviceDiscoveryConfigs));

        //var centralSwaggerConfigs = app.Configuration.GetSection("CentralSwagger").Get<object>();
        //app.Logger.LogInformation("CentralSwagger configs: {config}", JsonSerializer.Serialize(centralSwaggerConfigs));

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
