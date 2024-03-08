namespace Microsoft.Extensions.DependencyInjection;

internal static class CustomReverseProxyWebApplicationExtensions
{
    internal static void UseCustomReverseProxy(this WebApplication app)
    {
        app.MapReverseProxy();
    }
}
