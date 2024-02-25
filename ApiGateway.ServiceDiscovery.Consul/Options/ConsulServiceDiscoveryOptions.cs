namespace AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Options;

public partial class ConsulServiceDiscoveryOptions
{
    public ConsulClientOptions ConsulClient { get; set; }
    public ConsulServiceRegistrationOptions ServiceRegistration { get; set; }
}