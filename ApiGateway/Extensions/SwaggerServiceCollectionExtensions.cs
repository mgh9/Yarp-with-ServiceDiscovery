using ApiGateway.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AtiyanSeir.B2B.ApiGateway.Extensions;

internal static class SwaggerServiceCollectionExtensions
{
    internal static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
    }
}
