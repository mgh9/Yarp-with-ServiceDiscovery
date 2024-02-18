namespace Api1;

public static class Endpoints
{
    internal static void UseSampleEndpoints(this WebApplication app, IWebHostEnvironment environment)
    {
        app.MapGet("/GetApi01Data", () =>
        {
            return $"{DateTime.Now} - API 01 DATA";
        })
            .WithName("GetApi01Data")
            .WithOpenApi();
    }
}
