using ApiGateway.ServiceDiscovery.Consul;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Swagger.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ReverseProxyServiceCollectionExtensions
{
    internal static void AddCustomReverseProxy(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        services.AddReverseProxy()
                    .ConfigureHttpClient((context, handler) =>
                    {
                        //if (builder.Environment.IsDevelopment())
                        {
                            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, chainErrors) => true;
                        }
                    })
                    .LoadFromConsul()
                    .AddSwagger(EmptyReverseProxyDocumentFilterConfig);
                    //.AddSwagger((GetSwaggerConfig(reverseProxy.Clusters));
    }

    private static ReverseProxyDocumentFilterConfig EmptyReverseProxyDocumentFilterConfig
    {
        get
        {
            return new()
            {
                Clusters = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster>(),
                Routes = new Dictionary<string, RouteConfig>()
            };
        }
    }
}

