using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

//builder.Services.AddSingleton<IConsulClient, ConsulClient>(c => new ConsulClient(consulConfig =>
//{
//    consulConfig.Address = new Uri(builder.Configuration["ServiceDiscovery:Consul:Address"]);
//}));

builder.Services.RegisterWithConsulServiceDiscovery(builder.Configuration.GetSection("ConsulServiceDiscovery"));

builder.Services.AddCors();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

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

app.MapGet("/api02Test", () =>
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
.WithName("GetApi02TestData")
.WithOpenApi();


//app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
//{
//    // AllowCachingResponses = true,

//});

app.UseHealthChecks("/status", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var json = JsonSerializer.Serialize(new
        {
            Status = report.Status.ToString(),
            Environment = builder.Environment.EnvironmentName,
            Application = builder.Environment.ApplicationName,
            Platform = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription + " - " + RuntimeInformation.OSArchitecture,
        });

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }
});



app.MapControllers();

//app.AddConsulServiceDiscovery(app.Configuration.GetRequiredSection("ServiceDiscovery").Get<ServiceDiscoveryOptions>()!, app.Lifetime);



app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

