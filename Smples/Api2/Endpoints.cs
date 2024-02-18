using System.Runtime.InteropServices;
using System.Text.Json;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Api2;

public static class Endpoints
{
    internal static void UseSampleEndpoints(this WebApplication app, IWebHostEnvironment environment)
    {
        app.MapGet("/GetApi02Data", () =>
        {
            return $"{DateTime.Now} - API 02 DATA";
        })
            .WithName("GetApi02Data")
            .AddEndpointFilter<LBInfoFilter>()
            .WithOpenApi();

        app.UseHealthChecks("/status", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var json = JsonSerializer.Serialize(new
                {
                    Status = report.Status.ToString(),
                    Environment = environment.EnvironmentName,
                    Application = environment.ApplicationName,
                    Platform = RuntimeInformation.FrameworkDescription,
                    OS = RuntimeInformation.OSDescription + " - " + RuntimeInformation.OSArchitecture,
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }
        });
    }
}
