namespace AtiyanSeir.B2B.ApiGateway.Swagger;

public static class ModifySwaggerOperationsUrlMiddlewareExtensions
{
    public static IApplicationBuilder UseModifySwaggerOperationsUrlMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SwaggerOperationsUrlModifierMiddleware>();
    }
}
