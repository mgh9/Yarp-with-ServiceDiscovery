using System.Net;
using System.Text.Json;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using MyShared;
using Yarp.ReverseProxy.Configuration;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConsulServiceDiscoveryServiceCollectionExtensions
{
    public static IApplicationBuilder AddConsulServiceDiscovery(this IApplicationBuilder app
                                                                    , ServiceDiscoveryOptions serviceDiscoveryOptions
                                                                    , IHostApplicationLifetime lifetime
                                                                    , Serilog.ILogger? logger = null)
    {
        logger ??= app.ApplicationServices.GetService<Serilog.ILogger>();
        logger?.Information("Adding service with the discovery options: {options}", JsonSerializer.Serialize(serviceDiscoveryOptions));

        ValidateOptions(serviceDiscoveryOptions);

        // var serviceDiscoveryOptions = app.Configuration.GetSection("ServiceDiscovery").Get<ServiceDiscoveryOptions>();

        // Retrieve Consul client from DI
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

        AgentServiceCheck[] checks = PrepareServiceChecks(serviceDiscoveryOptions).ToArray();
        AgentServiceRegistration registration = PrepareServiceRegistration(serviceDiscoveryOptions, checks);

        consulClient.Agent.ServiceDeregister(registration.ID).Wait();
        consulClient.Agent.ServiceRegister(registration).Wait();

        HandleOnServiceLifetimeEvents(lifetime, consulClient, registration);

        return app;
    }

    private static void ValidateOptions(ServiceDiscoveryOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options?.ServiceAddress))
        {
            throw new ArgumentException($"{options?.ServiceAddress} is null");
        }
    }

    private static void HandleOnServiceLifetimeEvents(IHostApplicationLifetime lifetime, IConsulClient consulClient, AgentServiceRegistration registration)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
        });
    }

    private static AgentServiceRegistration PrepareServiceRegistration(ServiceDiscoveryOptions serviceDiscoveryOptions, AgentServiceCheck[] checks)
    {
        var localIpAddress = GetLocalIpAddress();

        return new AgentServiceRegistration()
        {
            ID = $"{serviceDiscoveryOptions.ServiceId}-{serviceDiscoveryOptions.ServicePort}",
            Name = serviceDiscoveryOptions.ServiceName,

            Address = localIpAddress,//serviceDiscoveryOptions.ServiceAddress,
            Port = serviceDiscoveryOptions.ServicePort,

            Checks = checks,

            Tags = serviceDiscoveryOptions.Tags,
            //Tags = GetRouteAndClusterTags(serviceDiscoveryOptions).ToArray(),
            Meta = GetRouteAndClusterTags(serviceDiscoveryOptions),
        };
    }

    private static Dictionary<string, string> GetRouteAndClusterTags(ServiceDiscoveryOptions serviceDiscoveryOptions)
    {
        var metadata = new Dictionary<string, string>();

        // Generate default route and cluster configurations
        var defaultRoute = new RouteConfig
        {
            RouteId = "defaulttttt_1",
            ClusterId = "CLUSTERRRRR_IDDD_1",
            //PathPrefix ="/api",
            Match = new RouteMatch { Path = "/api" }, // Example route pattern
            Metadata = new Dictionary<string, string>() // You can add metadata if needed
        };

        var defaultCluster = new ClusterConfig
        {
            ClusterId = "CLUSTERRRRR_IDDD_1",

            Destinations = new Dictionary<string, DestinationConfig>()
                                {
                                    { "serv_1", new DestinationConfig { Address = serviceDiscoveryOptions.HealthChecks.HttpsUrl } }
                                },
            Metadata = new Dictionary<string, string>() // You can add metadata if needed
        };

        // Convert default route and cluster to JSON strings and include them as metadata
        metadata["Routes"] = Newtonsoft.Json.JsonConvert.SerializeObject(new List<RouteConfig> { defaultRoute });
        metadata["Clusters"] = Newtonsoft.Json.JsonConvert.SerializeObject(new List<ClusterConfig> { defaultCluster });

        return metadata;
    }


    private static List<AgentServiceCheck> PrepareServiceChecks(ServiceDiscoveryOptions serviceDiscoveryOptions)
    {
        List<AgentServiceCheck> checks = [];

        var tcpCheck = new AgentServiceCheck()
        {
            TCP = $"{serviceDiscoveryOptions.ServiceAddress}:{serviceDiscoveryOptions.ServicePort}",
            Interval = TimeSpan.FromSeconds(serviceDiscoveryOptions.HealthChecks.IntervalSeconds),
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceDiscoveryOptions.HealthChecks.TimeoutSeconds),
        };
        checks.Add(tcpCheck);

        if (string.IsNullOrWhiteSpace(serviceDiscoveryOptions.HealthChecks.HttpUrl) == false)
        {
            var httpCheck = new AgentServiceCheck()
            {
                HTTP = $"{serviceDiscoveryOptions.HealthChecks.HttpUrl}",
                Interval = TimeSpan.FromSeconds(serviceDiscoveryOptions.HealthChecks.IntervalSeconds),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceDiscoveryOptions.HealthChecks.TimeoutSeconds),
            };

            checks.Add(httpCheck);
        }

        if (string.IsNullOrWhiteSpace(serviceDiscoveryOptions.HealthChecks.HttpsUrl) == false)
        {
            var httpsCheck = new AgentServiceCheck()
            {
                HTTP = $"{serviceDiscoveryOptions.HealthChecks.HttpsUrl}",
                Interval = TimeSpan.FromSeconds(serviceDiscoveryOptions.HealthChecks.IntervalSeconds),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceDiscoveryOptions.HealthChecks.TimeoutSeconds),
                TLSSkipVerify = true,
            };

            checks.Add(httpsCheck);
        }

        return checks;
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("Local IP Address not found!");
    }
}
