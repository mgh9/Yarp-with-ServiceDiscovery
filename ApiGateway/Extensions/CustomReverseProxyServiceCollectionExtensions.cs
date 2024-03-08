using Yarp.ServiceDiscovery.Abstractions;
using Yarp.ServiceDiscovery.Consul;

namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomReverseProxyServiceCollectionExtensions
{
    internal static IServiceCollection AddCustomReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        services.AddReverseProxy()
                    .ConfigureHttpClient((context, handler) =>
                    {
                        //if (builder.Environment.IsDevelopment())
                        {
                            // TODO: need to skip ssl/certificates issues in Development and/or Production?
                            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, chainErrors) => true;
                        }
                    })
                    .LoadFromConsul(configuration);

        return services;
    }
}

