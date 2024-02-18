namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul;

public class ConsulServiceDiscoveryOptions
{
    public ConsulClientOptions ConsulClient { get; set; }
    public ServiceRegistrationOptions ServiceRegistration { get; set; }
    public string[] Tags { get; set; }

    public class ConsulClientOptions
    {
        public string Host { get; set; }
        public string Datacenter { get; set; }
    }

    public class ServiceRegistrationOptions
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }
}