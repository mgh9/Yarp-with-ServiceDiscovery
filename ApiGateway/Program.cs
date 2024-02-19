using System.Runtime.InteropServices;
using System.Text.Json;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });
        builder.Services.AddHealthChecks();

        builder.Services.AddConsulClient(builder.Configuration.GetSection("ConsulServiceDiscovery:Client"));
        builder.Services.AddCustomSwagger();
        builder.Services.AddCustomReverseProxy(builder.Configuration);

        var app = builder.Build();

        app.UseHttpLogging();
        app.UseRouting();
        app.MapReverseProxy();

        app.UseSwaggerIfNotProduction();

        //app.UseHealthChecks("/status", new HealthCheckOptions
        //{
        //    ResponseWriter = async (context, report) =>
        //    {
        //        var json = JsonSerializer.Serialize(new
        //        {
        //            Status = report.Status.ToString(),
        //            Environment = builder.Environment.EnvironmentName,
        //            Application = builder.Environment.ApplicationName,
        //            Platform = RuntimeInformation.FrameworkDescription
        //        });

        //        context.Response.ContentType = "application/json";
        //        await context.Response.WriteAsync(json);
        //    }
        //});

        app.MapGet("/Reload", async context =>
        {
            var serviceDiscovery = context.RequestServices.GetRequiredService<IServiceDiscovery>();
            await serviceDiscovery.ReloadRoutesAndClustersAsync(default);
        })
            .WithName("Update Routes")
            .WithOpenApi();

        app.MapGet("/", async context =>
        {
            await context.Response.WriteAsync($"{app.Environment.ApplicationName} is here");
        });

        app.Run();
    }
}