namespace Yarp.ServiceDiscovery.Consul.Options;

public class ServiceInfoOptions
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Scheme { get; set; }
    public int Port { get; set; }

    public bool YarpIsEnabled { get; internal set; }
    public string? YarpRouteMatchPath { get; internal set; }
    public string? YarpRouteTransformPath { get; internal set; }

    public string? ServiceHealthCheckEndpoint { get; internal set; }
    public int ServiceHealthCheckSeconds { get; internal set; }
    public int ServiceHealthCheckTimeoutSeconds { get; internal set; }
    public int ServiceHealthCheckDeregisterSeconds { get; internal set; }

    public Dictionary<string, string> Meta { get; set; }
    public string[] Tags { get; set; }
}