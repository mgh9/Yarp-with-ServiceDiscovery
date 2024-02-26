using System.Text.Json;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Options;

namespace AtiyanSeir.B2B.ApiGateway.Extensions
{
    internal static class CustomApplicationExtensions
    {
        internal static void LogImportantConfigs(this WebApplication app)
        {
            var logs = $"----------------------------------------------{Environment.NewLine}";
            
            logs += $"Environment is {app.Environment.EnvironmentName}{Environment.NewLine}";

            var isCentralSwaggerEnabled = app.Configuration.GetValue<bool>("CentralSwagger:IsEnabled");
            var isCentralSwaggerEnabledText = isCentralSwaggerEnabled ? "enabled" : "disabled";
            logs += $"Central Swagger is `{isCentralSwaggerEnabledText}`{Environment.NewLine}";


            var consulClientOptions = app.Configuration.GetSection("ConsulServiceDiscovery:ConsulClient").Get<ConsulClientOptions>();
            logs += $"ConsulServiceDiscovery ConsulClient options: {JsonSerializer.Serialize(consulClientOptions)}{Environment.NewLine}";

            var isAutoDiscoveryEnabled = app.Configuration.GetValue<bool>("ConsulServiceDiscovery:AutoDiscovery:IsEnabled");
            var isAutoDiscoveryEnabledText = isAutoDiscoveryEnabled ? "enabled" : "disabled";
            logs += $"AutoDiscovery is `{isAutoDiscoveryEnabledText}`{Environment.NewLine}";

            logs += $"----------------------------------------------{Environment.NewLine}";

            app.Logger.LogInformation("{logs}", logs);
        }
    }
}
