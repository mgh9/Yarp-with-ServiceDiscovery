using Newtonsoft.Json.Linq;
using Yarp.ServiceDiscovery.Abstractions;

namespace Yarp.Swagger;

public class SwaggerOperationsUrlModifierMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceDiscovery _serviceDiscovery;

    public SwaggerOperationsUrlModifierMiddleware(RequestDelegate next, IServiceDiscovery serviceDiscovery)
    {
        _next = next;
        _serviceDiscovery = serviceDiscovery;
    }

    private static bool IsSwaggerUi(PathString path)
    {
        return path.ToUriComponent().EndsWith("swagger.json", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Invoke(HttpContext context)
    {
        if (!IsSwaggerUi(context.Request.Path))
        {
            await _next(context);
            return;
        }

        //SetAllowLoadingInsecureContentInSwaggerHtmlIndex(context);

        var originBody = context.Response.Body;

        try
        {
            var routes = _serviceDiscovery.GetRoutes();
            var clusters = _serviceDiscovery.GetClusters();

            var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context).ConfigureAwait(false);

            memStream.Position = 0;
            var responseBody = new StreamReader(memStream).ReadToEnd();

            responseBody = ChangeOperationsBaseUrlToPassFromReverseProxyUrl(context, routes, responseBody);
            await WriteModifiedResponseToOriginalBodyStreamAsync(originBody, responseBody).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = originBody;
        }
    }

    private static void SetAllowLoadingInsecureContentInSwaggerHtmlIndex(HttpContext context)
    {
        // Set Content Security Policy header to allow loading insecure content
        context.Response.Headers.ContentSecurityPolicy = "upgrade-insecure-requests";
    }

    private static async Task WriteModifiedResponseToOriginalBodyStreamAsync(Stream originBody, string responseBody)
    {
        var memoryStreamModified = new MemoryStream();
        var sw = new StreamWriter(memoryStreamModified);

        sw.Write(responseBody);
        sw.Flush();

        memoryStreamModified.Position = 0;
        await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="routes"></param>
    /// <param name="clusters"></param>
    /// <param name="responseBody"></param>
    /// <returns></returns>
    /// <example>
    /// For example if the original swagger.json response has a `paths` value like this :
    /// "paths": { "/GetOrderData": ... }
    /// (Assume the `GetOrderData` is an endpoint of a simple API like `MySimpleApi` behind the reverse proxy)
    /// After this method executed, it becomes this:
    /// "paths": { "/MySimpleApi/GetOrderData": ... }
    /// </example>
    private static string ChangeOperationsBaseUrlToPassFromReverseProxyUrl(HttpContext context, IReadOnlyList<ReverseProxy.Configuration.RouteConfig> routes, string responseBody)
    {
        // https://stackoverflow.com/questions/44508028/modify-middleware-response
        var arrayContainsServiceName = context.Request.Path.Value.Split('/');
        var serviceName = arrayContainsServiceName[2]!;

        var routeId = $"{serviceName}-route"!;
        var mainRoute = routes.SingleOrDefault(x => x.RouteId == routeId);
        var routePathWithoutReminder = mainRoute.Match.Path.Replace("/{**remainder}", "");

        JObject jObject = JObject.Parse(responseBody);
        var apiName = jObject["info"]["title"].ToString();

        // Get the paths object
        JObject paths = (JObject)jObject["paths"];

        // Create a new paths object
        JObject newPaths = [];

        foreach (JProperty property in paths.Properties())
        {
            string newName = $"{routePathWithoutReminder}{property.Name}";

            newPaths.Add(newName, property.Value);
        }

        jObject["paths"] = newPaths;

        string modifiedJson = jObject.ToString();
        responseBody = modifiedJson;

        return responseBody;
    }
}
