namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomSwaggerServiceCollectionExtensions
{
    internal static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}
