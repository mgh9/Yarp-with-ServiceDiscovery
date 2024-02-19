using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;

namespace Api1;

public static class Endpoints
{
    internal static void UseSampleEndpoints(this WebApplication app, IWebHostEnvironment environment)
    {
        app.MapGet("/GetApi01Data", (HttpContext context) =>
        {
            var request = context.Request;
            var host = request.Host;
            var scheme = request.Scheme;

            // Log the endpoint information
            app.Logger.LogWarning($"API 01 Endpoint called: {scheme}://{host}{request.Path}");

            return $"{DateTime.Now} - API 01 DATA";
        })
            .WithName("GetApi01Data")
            .AddEndpointFilter<LBInfoFilter>()
            .WithOpenApi();
    }
}
