using ApiGateway.ServiceDiscovery.Consul;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomReverseProxyServiceCollectionExtensions
{
    internal static void AddCustomReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        services.AddReverseProxy()
                    .ConfigureHttpClient((context, handler) =>
                    {
                        // TODO: do we need this in production or just the development?
                        //if (builder.Environment.IsDevelopment())
                        {
                            // need to skip ssl/certificates issues
                            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, chainErrors) => true;
                        }
                    })
                    .LoadFromConsul(configuration);
    }
}

