using System.Runtime.InteropServices;
using System.Text.Json;
using ApiGateway.ServiceDiscovery.Abstractions;
using ApiGateway.ServiceDiscovery.Consul;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //builder.Services.ConfigureHttpClientDefaults(client =>
        //{
        //    //client.AddStandardResilienceHandler();
        //});

        //builder.Services.AddHttpClient();
        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });
        builder.Services.AddHealthChecks();

        // Configure Consul client (adjust as needed)
        builder.Services.AddConsulClient(builder.Configuration.GetSection("ConsulServiceDiscovery:Client"));
        builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        //// Add services to the container.
        //// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        //builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();
        builder.Services.AddCustomSwagger();

        //builder.Services.AddReverseProxy()
        //                .ConfigureHttpClient((context, handler) =>
        //                    {
        //                        if (builder.Environment.IsDevelopment())
        //                        {
        //                            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, chainErrors) => true;
        //                        }
        //                    })
        //                    .LoadFromConsul();
        builder.Services.AddCustomReverseProxy(builder.Configuration);


        builder.Services.AddCors();
        var app = builder.Build();

        app.UseCors(builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
        
        //////// Configure the HTTP request pipeline.
        //////if (app.Environment.IsProduction() == false)
        //////{
        //////    app.UseSwagger();
        //////    app.UseSwaggerUI();
        //////}
        app.PrepareSwaggerIfNotProduction();

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

        //app.MapGet("/Reload", async (HttpContext context, ILogger logger) =>
        app.MapGet("/Reload", async context =>
        {
            //logger.LogInformation("Reloading manually...");

            var serviceDiscovery = context.RequestServices.GetRequiredService<IServiceDiscovery>();
            await serviceDiscovery.ReloadRoutesAndClustersAsync(default);
        })
            .WithName("Update Routes")
            .WithOpenApi();

        app.UseHttpLogging();


        app.UseHealthChecks("/status", new HealthCheckOptions
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

        // Use custom provider to load clusters and routes
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            //endpoints.MapReverseProxy();
        });

        // Enable endpoint routing, required for the reverse proxy
        app.MapGet("/", async ctx =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var payload = new
            {
                AvailableRoutes = new[]
                {
                    $"{baseUrl}/items"
                }
            };

            await ctx.Response.WriteAsJsonAsync(payload, new JsonSerializerOptions { WriteIndented = true });
        });

        app.MapReverseProxy(proxyPipeline =>
        {
            // Use a custom proxy middleware, defined below
            //proxyPipeline.Use(MyCustomProxyStep);
            // Don't forget to include these two middleware when you make a custom proxy pipeline (if you need them).
            //proxyPipeline.UseSessionAffinity();
            //proxyPipeline.UseLoadBalancing();
        });

        app.Run();
    }


}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

