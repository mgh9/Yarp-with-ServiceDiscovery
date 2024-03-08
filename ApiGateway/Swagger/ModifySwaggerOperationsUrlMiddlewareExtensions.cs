namespace Yarp.Swagger;

internal static class ModifySwaggerOperationsUrlMiddlewareExtensions
{
    internal static IApplicationBuilder UseModifySwaggerOperationsUrlMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SwaggerOperationsUrlModifierMiddleware>();
    }
}
