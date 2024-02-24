using System.Runtime.InteropServices;
using System.Text.Json;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
//builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.Request; });
builder.Services.AddHealthChecks();

var host = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Host") ?? throw new ArgumentException("Consul server address or not found!");
var datacenter = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Datacenter") ?? string.Empty;
builder.Services.AddConsulClient(new AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.ConsulServiceDiscoveryOptions.ConsulClientOptions(host, datacenter));

builder.Services.AddCustomSwagger();
builder.Services.AddCustomReverseProxy();

var app = builder.Build();

//app.UseHttpLogging();
app.UseRouting();
app.MapReverseProxy();

app.UseSwaggerIfNotProduction();
app.UseModifySwaggerResponse();

app.UseHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var json = JsonSerializer.Serialize(new
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

app.MapGet("/Reload", async context =>
{
    var serviceDiscovery = context.RequestServices.GetRequiredService<IServiceDiscovery>();
    await serviceDiscovery.ReloadRoutesAndClustersAsync(default);
})
    .WithName("Reload/Update Routes and Clusters")
    .WithOpenApi();

app.MapGet("/info", async context =>
{
    var appVersion = typeof(Program).Assembly.GetName().Version.ToString();
    await context.Response.WriteAsync($"{app.Environment.ApplicationName} is here. Version: {appVersion}");
});

app.Run();
