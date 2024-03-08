using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Yarp.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomEndpointsWebApplicationExtensions
{
    internal static void MapCustomEndpoints(this WebApplication app, WebApplicationBuilder builder)
    {
        app.UseHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = report.Status.ToString(),
                    Environment = builder.Environment.EnvironmentName,
                    Application = builder.Environment.ApplicationName,
                    Platform = RuntimeInformation.FrameworkDescription
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }
        });

        // TODO: make it as POST and can be called from only internal network (white-listed)
        app.MapGet("/reload", async context =>
        {
            var serviceDiscovery = context.RequestServices.GetRequiredService<IServiceDiscovery>();
            await serviceDiscovery.ReloadAsync(default);
            await context.Response.WriteAsync("Reloaded");
        })
            .WithName("Reload/Update Routes and Clusters")
            .WithOpenApi();

        app.MapGet("/info", async context =>
        {
            var appVersion = typeof(Program).Assembly.GetName().Version.ToString();
            await context.Response.WriteAsync($"{app.Environment.ApplicationName} is here. Version: {appVersion}");
        });

        app.MapGet("/configs", async (IServiceDiscovery serviceDiscovery, HttpContext context) =>
        {
            var allRoutesAndClustersJsoned = serviceDiscovery.ExportConfigs();
            await context.Response.WriteAsync(allRoutesAndClustersJsoned);
        });

        app.MapGet("/", async context =>
        {
            await context.Response.WriteAsync($"{app.Environment.ApplicationName} is here.");
        });
    }
}