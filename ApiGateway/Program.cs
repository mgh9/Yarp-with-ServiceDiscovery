using System.Text.Json;
using ApiGateway.Extensions;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Yarp.ReverseProxy.Configuration;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient();
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders;
        });

        // Configure Consul client (adjust as needed)
        builder.Services.AddSingleton<IConsulClient>(c => new ConsulClient(config =>
        {
            config.Address = new Uri("http://localhost:8500");
        }));

        //////builder.Services.AddSingleton<IProxyConfigProvider, MyCustomProxyConfigProvider>()
        //////                 .AddReverseProxy();
        //builder.Services.AddReverseProxy().LoadFromMessages(x => { }); // No static configuration
        //.LoadFromConfig(_configuration.GetSection("ReverseProxy"));

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        //builder.Services.AddReverseProxy().LoadFromMemory(GetRoutes(), await GetClustersAsync(builder.Services));
        var routesClusters = GetRoutesAndClustersAsync(builder.Services).Result;
        builder.Services.AddReverseProxy()
                            .LoadFromMemory(routesClusters.Item1, routesClusters.Item2)
                            .ConfigureHttpClient((context, handler)=> 
                            {
                                handler.SslOptions.RemoteCertificateValidationCallback=   HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                            });

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
            //endpoints.MapReverseProxy();
        });

        app.UseHttpLogging();
        // Enable endpoint routing, required for the reverse proxy

        //app.MapReverseProxy();
        app.MapReverseProxy(proxyPipeline =>
        {
            // Use a custom proxy middleware, defined below
            proxyPipeline.Use(MyCustomProxyStep);
            // Don't forget to include these two middleware when you make a custom proxy pipeline (if you need them).
            proxyPipeline.UseSessionAffinity();
            proxyPipeline.UseLoadBalancing();
        });

        app.Run();







        async Task<IReadOnlyList<ClusterConfig>> GetClustersAsync(IServiceCollection services)
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
                                //Address = await GetUrlFromServiceDiscoveryByName("API 1", services)
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

        //static async Task<string> GetUrlFromServiceDiscoveryByName(string name, IServiceCollection servicessss)
        //{
        //    //var routesAndClusters = await GetRoutesAndClustersAsync(consullll);


        //    //IConsulClient consulClient
        //    //var consulClient = new ConsulClient();
        //    //var services = await consulClient.Catalog.Service(name);
        //    //var service = services.Response?.First();

        //    //if (service == null) return string.Empty;

        //    //return $"http://{service.ServiceAddress}:{service.ServicePort}";
        //}

        static async Task<(List<RouteConfig>, List<ClusterConfig>)> GetRoutesAndClustersAsync(IServiceCollection servicessss)
        {
            var consulClient = servicessss.BuildServiceProvider().GetService<IConsulClient>();

            var getServicesFromConsulResult = await consulClient.Agent.Services();
            var discoveredServices = getServicesFromConsulResult.Response;

            List<RouteConfig> routes = new();
            List<ClusterConfig> clusters = new();

            foreach (var item in discoveredServices)
            {
                var routesJson = item.Value.Meta["Routes"];
                var clustersJson = item.Value.Meta["Clusters"];

                routes = JsonSerializer.Deserialize<List<RouteConfig>>(routesJson)!;
                clusters = JsonSerializer.Deserialize<List<ClusterConfig>>(clustersJson)!;
            }

            return (routes, clusters);
        }


        Task MyCustomProxyStep(HttpContext context, Func<Task> next)
        {
            // Can read data from the request via the context
            foreach (var header in context.Request.Headers)
            {
                Console.WriteLine($"{header.Key}: {header.Value}");
            }

            // The context also stores a ReverseProxyFeature which holds proxy specific data such as the cluster, route and destinations
            var proxyFeature = context.GetReverseProxyFeature();
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(proxyFeature.Route.Config));

            // Important - required to move to the next step in the proxy pipeline
            return next();
        }
    }


}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

