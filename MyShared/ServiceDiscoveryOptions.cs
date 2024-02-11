namespace MyShared;

public class ServiceDiscoveryOptions
{
    public ConsulServiceDiscoveryOptions Consul { get; set; }

    public string ServiceAddress { get; set; }
    public int ServicePort { get; set; }
    public string ServiceName { get; set; }
    public string ServiceId { get; set; }

    public string[] Tags { get; set; }

    public ServiceHealthCheckOptions HealthChecks { get; set; }
}

public class ConsulServiceDiscoveryOptions
{
    public string Address { get; set; }
}

public class ServiceHealthCheckOptions
{
    public int IntervalSeconds { get; set; }
    public int TimeoutSeconds { get; set; }

    public string HttpUrl { get; set; }
    public string HttpsUrl { get; set; }
}
