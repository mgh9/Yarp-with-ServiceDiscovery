using AtiyanSeir.B2B.ApiGateway.Swagger;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

internal static class SwaggerServiceCollectionExtensions
{
    internal static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
    }
}
