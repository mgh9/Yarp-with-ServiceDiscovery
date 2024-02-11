using Consul;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyShared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IConsulClient, ConsulClient>(c => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri(builder.Configuration["ServiceDiscovery:Consul:Address"]);
}));

builder.Services.AddControllers();

builder.Services.AddHealthChecks()
                    .AddCheck<MainHealthCheck>("MainHealthCheck");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // AllowCachingResponses = true,
});

app.MapControllers();

app.AddConsulServiceDiscovery(app.Configuration.GetRequiredSection("ServiceDiscovery").Get<ServiceDiscoveryOptions>()!, app.Lifetime);

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal class MainHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IHealthCheck> _checks;

    public MainHealthCheck(IEnumerable<IHealthCheck> checks)
    {
        _checks = checks;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_checks.Select(c => c.CheckHealthAsync(context, cancellationToken)));

        if (results.Any(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy))
        {
            return HealthCheckResult.Unhealthy();
        }

        return HealthCheckResult.Healthy();
    }
}
