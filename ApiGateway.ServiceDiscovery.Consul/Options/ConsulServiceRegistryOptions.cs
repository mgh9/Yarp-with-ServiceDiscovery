namespace Yarp.ServiceDiscovery.Consul.Options;

public partial class ConsulServiceRegistryOptions
{
    public ConsulClientOptions ConsulClient { get; set; }
    public ServiceInfoOptions ServiceInfo { get; set; }
}