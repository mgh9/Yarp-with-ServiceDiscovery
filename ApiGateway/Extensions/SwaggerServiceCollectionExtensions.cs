using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;
//public class CustomOperationFilter : IOperationFilter
//{
//    private readonly string tp;

//    public CustomOperationFilter(string tp)
//    {
//        this.tp = tp;
//    }

//    public void Apply(OpenApiOperation operation, OperationFilterContext context)
//    {
//        // Modify the base URL for operations
//        operation.Servers = new List<OpenApiServer>
//        {
//            new() { Url = "https://example.com/api" } // Change this URL to your desired base URL
//        };
//    }
//}

internal static class SwaggerServiceCollectionExtensions
{
    internal static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        //services.AddOpenApiDocument(config =>
        //{
        //    config.PostProcess = document =>
        //    {
        //        var sdsd = document.BaseUrl;
        //        var ff = document.DocumentPath;
        //        var ff3 = document.BasePath;

        //        document.Info.Title = "MYYYY GATEWAYY";
        //        document.Info.Description = "A simple ASP.NET Core web API";
        //    };
        //});
        //services.AddSwaggerDocument();


        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
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

        app.UseSwagger();
        //app.MapSwagger( pattern:"asgharrrSwagger/{documentName}/testttt.json",setupAction: x => 
        //{

        //});

        //app.UseOpenApi();

        //////app.UseSwaggerUI( c =>
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

            options.DocumentTitle = "AtiyanSeir API Gateway";
            var serviceDiscovery = app.Services.GetService<IServiceDiscovery>();
            options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery);
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


//tuye ye// branch e dg:
//       // ya kollan app e dg: az appsettings bekhun route o cluster ro ba 
//    // library e Treyt baraye YARP.
//    // aval khali bashe, badesh az Consul bkhun beriz tuye Appsettings bebin change apply mishe tuye runtime


//    ya
    // interceptor middleware bezan say kon bekhune response e tolid shode ro va taghiresh bedi