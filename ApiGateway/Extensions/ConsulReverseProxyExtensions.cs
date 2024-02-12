////using Consul;
//using Yarp.ReverseProxy.Configuration;

//namespace ApiGateway.Extensions
//{
//    public static class ConsulReverseProxyExtensions
//    {
//        public static IReverseProxyBuilder LoadFromConsul(this IReverseProxyBuilder builder)
//        {
//            builder.Services.AddSingleton<IProxyConfigProvider, ConsulProxyConfigProvider>();
//            return builder;
//        }
//    }
//}

using Yarp.ReverseProxy.Configuration;

public static class InMemoryConfigProviderExtensions
{
    /// <summary>
    /// Adds an InMemoryConfigProvider
    /// </summary>
    public static IReverseProxyBuilder LoadFromMemory(this IReverseProxyBuilder builder, IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        builder.Services.AddSingleton(new InMemoryConfigProvider(routes, clusters));
        builder.Services.AddSingleton<IProxyConfigProvider>(s => s.GetRequiredService<InMemoryConfigProvider>());
        return builder;
    }
}