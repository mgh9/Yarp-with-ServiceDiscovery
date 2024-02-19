using Yarp.ReverseProxy.Swagger;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AtiyanSeir.B2B.ApiGateway.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly ReverseProxyDocumentFilterConfig _reverseProxyDocumentFilterConfig;

    public ConfigureSwaggerOptions(IOptionsMonitor<ReverseProxyDocumentFilterConfig> reverseProxyDocumentFilterConfigOptions)
    {
        _reverseProxyDocumentFilterConfig = reverseProxyDocumentFilterConfigOptions.CurrentValue;
    }

    public void Configure(SwaggerGenOptions options)
    {
        var filterDescriptors = new List<FilterDescriptor>();

        foreach (var cluster in _reverseProxyDocumentFilterConfig.Clusters)
        {
            options.SwaggerDoc(cluster.Key, new OpenApiInfo { Title = cluster.Key, Version = cluster.Key });
        }

        filterDescriptors.Add(new FilterDescriptor
        {
            Type = typeof(ReverseProxyDocumentFilter),
            Arguments = []
        });

        options.DocumentFilterDescriptors = filterDescriptors;
    }
}
