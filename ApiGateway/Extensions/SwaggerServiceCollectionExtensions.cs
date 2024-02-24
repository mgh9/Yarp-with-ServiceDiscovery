using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;

namespace Microsoft.Extensions.DependencyInjection;

internal static class SwaggerServiceCollectionExtensions
{
    internal static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        //services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(c =>
        {
            // c.CustomOperationIds(e => "asgharrrrr");

            //c.SwaggerGeneratorOptions.SwaggerDocs.Add("sds", new OpenApiInfo { Title = "gg" });
            //c.OperationFilter<CustomOperationFilter>("test1111");
        });
    }

    /// <summary>
    /// Prepare central-swagger based on routes/clusters configurations
    /// </summary>
    /// <param name="app"></param>
    internal static void UseSwaggerIfNotProduction(this WebApplication app)
    {
        string baseApplicationRoute = "PROXYY";

        //if (app.Environment.IsProduction())
        //    return;

        app.UseSwagger(x =>
        {

        });
        //app.MapSwagger( pattern:"asgharrrSwagger/{documentName}/testttt.json",setupAction: x => 
        //{

        //});

        //app.UseOpenApi();

        //////app.UseSwagger( c =>
        //////{
        //////   // c.RouteTemplate = baseApplicationRoute + "/swagger/{documentName}/swagger.json";

        //////    ////////c.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
        //////    ////////{
        //////    ////////    var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscovery>();
        //////    ////////    var jsoned = serviceDiscovery.ExportConfigs();

        //////    ////////    //swaggerDoc.Info.
        //////    ////////    var clusters = serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

        //////    ////////    ////var srvs = new List<OpenApiServer>();
        //////    ////////    ////foreach (var cluster in clusters)
        //////    ////////    ////{
        //////    ////////    ////    //var f = cluster.Destinations.First();
        //////    ////////    ////    //var o = new OpenApiServer() { Url = f.Value.Address };
        //////    ////////    ////    ////options.SwaggerEndpoint($"/swagger/{cluster.ClusterId}/swagger.json", cluster.ClusterId);
        //////    ////////    ////    //swaggerDoc.Servers.Add(o);


        //////    ////////    ////    var op = new OpenApiPathItem();
        //////    ////////    ////    op.AddOperation(OperationType.Get, new OpenApiOperation() { Summary = "sss" });
        //////    ////////    ////    swaggerDoc.Paths.Add(cluster.ClusterId, new OpenApiPathItem(op));
        //////    ////////    ////}

        //////    ////////    if (!httpRequest.Headers.ContainsKey("X-Forwarded-Host"))
        //////    ////////        return;

        //////    ////////    var basePath = "proxy";
        //////    ////////    var serverUrl = $"{httpRequest.Scheme}://{httpRequest.Headers["X-Forwarded-Host"]}/{basePath}";
        //////    ////////    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = serverUrl } };
        //////    ////////});
        //////});

        app.UseSwaggerUI(options =>
        {
            //options.SwaggerEndpoint($"/{baseApplicationRoute}/swagger/v1/swagger.json", "My Cool API V1");
            //options.RoutePrefix = $"{baseApplicationRoute}/swagger";

            ////var config = app.Services.GetRequiredService<IOptionsMonitor<ReverseProxyDocumentFilterConfig>>().CurrentValue;
            ////foreach (var cluster in config.Clusters)
            ////{
            ////    options.SwaggerEndpoint($"/swagger/{cluster.Key}/swagger.json", cluster.Key);
            ////}
            //options..RoutePrefix = "/asgharrrrrrr22";

            options.DocumentTitle = "AtiyanSeir B2B API Gateway";
            var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscovery>();
            var httpContext = app.Services.GetRequiredService<IHttpContextAccessor>();

            options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery, httpContext, app.Logger);
            //options.ConfigObject.= new SwaggerEndpointEnumerator(serviceDiscovery);

            ////////var clusters = serviceDiscovery?.GetClusters() ?? new List<ClusterConfig>();

            ////////foreach (var cluster in clusters)
            ////////{
            //options.SwaggerEndpoint($"/{baseApplicationRoute}/swagger/v1/swagger.json", "sssss");
            ///////options.RoutePrefix = $"{baseApplicationRoute}/swagger";
            //options.ConfigObject.
            ////////}
        });
    }
}
