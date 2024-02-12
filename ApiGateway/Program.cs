using ApiGateway.Extensions;
using Consul;
using Yarp.ReverseProxy.Configuration;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient();

        // Configure Consul client (adjust as needed)
        builder.Services.AddSingleton<IConsulClient>(c => new ConsulClient(config =>
        {
            config.Address = new Uri("http://localhost:8500");
        }));

        builder.Services.AddSingleton<IProxyConfigProvider, MyCustomProxyConfigProvider>()
                         .AddReverseProxy();
        //.LoadFromConfig(_configuration.GetSection("ReverseProxy"));








        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        ////////////builder.Services.AddReverseProxy().LoadFromMemory(GetRoutes(), await GetClustersAsync());

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

        // Use custom provider to load clusters and routes
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapReverseProxy();
        });

        app.MapReverseProxy();

        app.Run();







        async Task<IReadOnlyList<ClusterConfig>> GetClustersAsync()
        {
            return new[]
            {
                new ClusterConfig()
                {
                    ClusterId = "cluster1",
                    SessionAffinity = new SessionAffinityConfig { Enabled = true, Policy = "Cookie", AffinityKeyName = ".Yarp.ReverseProxy.Affinity" },
                    Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            "api1", new Yarp.ReverseProxy.Configuration.DestinationConfig()
                            {
                                Address = await GetUrlFromServiceDiscoveryByName("API 1")
                            }
                        },
                    }
                }
            };
        }

        IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteConfig> GetRoutes()
        {
            return new[]
                   {
                new Yarp.ReverseProxy.Configuration.RouteConfig()
                {
                    RouteId = "route" + Random.Shared.Next(),
                    ClusterId = "cluster1",
                    Match = new RouteMatch
                    {
                        Path = "{**catch-all}"
                    }
                }
            };
        }

        static async Task<string> GetUrlFromServiceDiscoveryByName(string name)
        {
            var consulClient = new ConsulClient();
            var services = await consulClient.Catalog.Service(name);
            var service = services.Response?.First();

            if (service == null) return string.Empty;

            return $"http://{service.ServiceAddress}:{service.ServicePort}";
        }
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}