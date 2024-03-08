using Yarp.ServiceDiscovery.Abstractions;

namespace Api1;

public static class Endpoints
{
    internal static void UseSampleEndpoints(this WebApplication app, IWebHostEnvironment environment)
    {
        app.MapGet("/GetOrderData", (HttpContext context) =>
        {
            var request = context.Request;
            var host = request.Host;
            var scheme = request.Scheme;

            // Log the endpoint information
            app.Logger.LogWarning($"API 01 Endpoint called: {scheme}://{host}{request.Path}");

            return $"{DateTime.Now} - API 01 DATA";
        })
            .WithName("Order Get Data 1")
            .AddEndpointFilter<LBInfoFilter>()
            .WithOpenApi();

        app.MapGet("/GetOrderFullData", (HttpContext context) =>
        {
            var request = context.Request;
            var host = request.Host;
            var scheme = request.Scheme;

            // Log the endpoint information
            app.Logger.LogWarning($"API 01 Full Endpoint called: {scheme}://{host}{request.Path}");

            return $"{DateTime.Now} - API 01 Full DATA";
        })
            .WithName("OrderFullData")
            .AddEndpointFilter<LBInfoFilter>()
            .WithOpenApi();
    }
}
