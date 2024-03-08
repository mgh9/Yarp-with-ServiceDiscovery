using Yarp.ServiceDiscovery.Abstractions;
using Yarp.Swagger;

namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomSwaggerWebApplicationExtensions
{
    /// <summary>
    /// Prepare central-swagger based on routes/clusters configurations
    /// </summary>
    /// <param name="app"></param>
    internal static void UseCustomSwagger(this WebApplication app)
    {
        //if (app.Environment.IsProduction())

        var isCentralSwaggerEnabled = app.Configuration.GetValue<bool>("CentralSwagger:IsEnabled");
        if (!isCentralSwaggerEnabled)
        {
            app.Logger.LogDebug("Central swagger is disabled, so ignoring generating swagger documents");
            return;
        }

        app.Logger.LogDebug("Central swagger is enabled, so generating swagger documents based on routes/clusters...");
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "AtiyanSeir B2B API Gateway";
            var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscovery>();
            var httpContext = app.Services.GetRequiredService<IHttpContextAccessor>();
            options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery, httpContext, app.Logger);
        });

        app.UseModifySwaggerOperationsUrlMiddleware();
    }
}
