using Consul;
using Yarp.ReverseProxy.Configuration;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

namespace ApiGateway.Extensions
{
    public class MyCustomProxyConfigProvider : IProxyConfigProvider
    {
        private MyInMemoryConfig _config;
        private readonly IConsulClient _consulClient;
        private readonly CancellationTokenSource _stoppingToken = new();

        public MyCustomProxyConfigProvider(IConsulClient consulClient)
        {
            _consulClient = consulClient;

            //// Load a basic configuration
            //// Should be based on your application needs.
            //var routeConfig = new RouteConfig
            //{
            //    RouteId = "route1",
            //    ClusterId = "cluster1",
            //    Match = new RouteMatch
            //    {
            //        Path = "/api/service1/{**catch-all}"
            //    }
            //};

            //var routeConfigs = new[] { routeConfig };

            //var clusterConfigs = new[]
            //{
            //    new ClusterConfig
            //    {
            //        ClusterId = "cluster1",
            //        LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
            //        Destinations = new Dictionary<string, DestinationConfig>
            //        {
            //            { "destination1", new DestinationConfig { Address = "https://localhost:5001/" } },
            //            { "destination2", new DestinationConfig { Address = "https://localhost:5002/" } }
            //        }
            //    }
            //};

            //_config = new MyInMemoryConfig(routeConfigs, clusterConfigs);

            PeriodicUpdateAsync(_stoppingToken.Token);
        }

        public IProxyConfig GetConfig()
        {
            return _config;            
        }

        private async Task PeriodicUpdateAsync(CancellationToken stoppingToken)
        {
            try
            {
                do
                {
                    var delay = TimeSpan.FromSeconds(60);
                        //_serviceDiscoveryOptions.CurrentValue.PeriodicUpdateIntervalInSeconds);

                    Update();
                    await Task.Delay(delay, stoppingToken);

                } while (!stoppingToken.IsCancellationRequested);

            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// By calling this method from the source we can dynamically adjust the proxy configuration.
        /// Since our provider is registered in DI mechanism it can be injected via constructors anywhere.
        /// </summary>
        public void Update()//IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            var oldConfig = _config;

            var clusters = GetClustersAsync().Result;
            List<RouteConfig> routes = new();

            _config = new MyInMemoryConfig(routes, clusters);
            oldConfig.SignalChange();
        }

        async Task<IReadOnlyList<ClusterConfig>> GetClustersAsync()
        {
            var getServicesFromConsulResult = await _consulClient.Agent.Services();
            var discoveredServices = getServicesFromConsulResult.Response;

            List<ClusterConfig> clusters = new();

            //foreach (var cluster in GetConfig().Clusters)
            foreach (var item in discoveredServices)
            {
                var c = new ClusterConfig { ClusterId = item.Key };
                Dictionary<string, DestinationConfig> destinations = CreateDestinationsForCluster(c, discoveredServices);

                var newCluster = c with { Destinations = destinations };

                clusters.Add(newCluster);
            }

            return clusters;
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

        private Dictionary<string, DestinationConfig> CreateDestinationsForCluster(ClusterConfig defaultCluster,
         Dictionary<string, AgentService> discoveredServices)
        {
            Dictionary<string, DestinationConfig> destinations = new();

            if (defaultCluster.Destinations is null)
                return destinations;

            foreach (KeyValuePair<string, DestinationConfig> defaultDestination in defaultCluster.Destinations)
            {
                string defaultDestinationKey = defaultDestination.Key;
                DestinationConfig defaultDestinationValue = defaultDestination.Value;

                string? placeholder = ExtractDestinationServiceNamePlaceholder(defaultDestinationValue.Address);

                if (placeholder is null)
                {
                    destinations.Add(defaultDestinationKey, defaultDestinationValue);
                    continue;
                }

                List<AgentService> agentServices = discoveredServices.Values
                    .Where(s => s.Service.ToLowerInvariant() == placeholder.ToLowerInvariant())
                    .ToList();

                if (!agentServices.Any())
                    continue;

                foreach (AgentService agentService in agentServices)
                {
                    var destinationName = agentService.ID;

                    var healsEndpointExist = agentService.Meta.TryGetValue("HealthEndpoint", out var healthEndpoint);

                    var schemeExist = agentService.Meta.TryGetValue("Scheme", out var scheme);

                    if (!schemeExist)
                        continue;

                    var address = $"{scheme}://{agentService.Address}:{agentService.Port}";

                    DestinationConfig destinationConfig = new()
                    {
                        Address = defaultDestinationValue.Address.Replace($"[{placeholder}]", address),
                        Health = healthEndpoint,
                        Metadata = defaultDestinationValue.Metadata
                    };

                    destinations.Add(destinationName, destinationConfig);
                }
            }

            return destinations;
        }


        private string? ExtractDestinationServiceNamePlaceholder(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var startIndex = url.IndexOf('[', StringComparison.Ordinal) + 1;

            if (startIndex == 0)
                return null;


            var endIndex = url.IndexOf(']', startIndex);

            if (endIndex == -1)
                return null;


            return url.Substring(startIndex, length: endIndex - startIndex);

        }

        string GetUrlFromServiceDiscoveryByName(string name)
        {
            //var consulClient = new ConsulClient();
            var services = _consulClient.Catalog.Service(name).Result;
            var service = services.Response?.First();

            if (service == null) return string.Empty;

            return $"http://{service.ServiceAddress}:{service.ServicePort}";
        }
    }
}
