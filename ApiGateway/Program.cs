using System.Text;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;
using Microsoft.AspNetCore.Rewrite;

namespace AtiyanSeir.B2B.ApiGateway;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddLogging(opt =>
        // {
        //     opt.AddConsole(c =>
        //     {
        //         c.TimestampFormat = "[HH:mm:ss] ";
        //     });
        // });

        builder.Services.AddHttpContextAccessor();
        //builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.Request; });
        builder.Services.AddHealthChecks();

        var host = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Host") ?? throw new ArgumentException("Consul server address or not found!");
        var datacenter = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Datacenter") ?? string.Empty;
        builder.Services.AddConsulClient(new AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.ConsulServiceDiscoveryOptions.ConsulClientOptions(host, datacenter));

        builder.Services.AddCustomSwagger();
        builder.Services.AddCustomReverseProxy();

        var app = builder.Build();


        //app.UseHttpLogging();
        app.UseRouting();
        app.MapReverseProxy();

        app.UseSwaggerIfNotProduction();
        //app.UseRewriter(CreateSwaggerRewriteOptions());

        app.UseModifySwaggerResponse();

        //app.UseHealthChecks("/status", new HealthCheckOptions
        //{
        //    ResponseWriter = async (context, report) =>
        //    {
        //        var json = JsonSerializer.Serialize(new
        //        {
        //            Status = report.Status.ToString(),
        //            Environment = builder.Environment.EnvironmentName,
        //            Application = builder.Environment.ApplicationName,
        //            Platform = RuntimeInformation.FrameworkDescription
        //        });

        //        context.Response.ContentType = "application/json";
        //        await context.Response.WriteAsync(json);
        //    }
        //});

        app.MapGet("/Reload", async context =>
        {
            var serviceDiscovery = context.RequestServices.GetRequiredService<IServiceDiscovery>();
            await serviceDiscovery.ReloadRoutesAndClustersAsync(default);
        })
            .WithName("Update Routes")
            .WithOpenApi();

        app.MapGet("/", async context =>
        {
            await context.Response.WriteAsync($"{app.Environment.ApplicationName} is here");
        });

        //app.MapGet("/custom-swagger-json", async context =>
        //{
        //    var originalSwaggerString = await (context.RequestServices.GetRequiredService<ISwaggerProvider>().GetSwagger("mainn")..GetSwaggerDocumentAsync());

        //    // Modify the Swagger JSON string here
        //    var modifiedSwaggerString = ModifySwaggerOperationsUrls(originalSwaggerString);

        //    context.Response.ContentType = "application/json";
        //    await context.Response.WriteAsync(modifiedSwaggerString);
        //});


        app.Run();
    }

    private static RewriteOptions CreateSwaggerRewriteOptions()
    {
        var rewriteOptions = new RewriteOptions();

        rewriteOptions.Add(context =>
        {
            // Check if the request path matches the Swagger JSON endpoint
            if (context.HttpContext.Request.Path.StartsWithSegments("/swagger-json"))
            {
                // Read the original response body
                var originalBody = context.HttpContext.Response.Body;
                using (var memoryStream = new MemoryStream())
                {
                    context.HttpContext.Response.Body = memoryStream;

                    // Process the request
                    //context.Next();

                    // Modify the Swagger operations' URLs
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var responseBody = new StreamReader(memoryStream).ReadToEnd();
                    responseBody = ModifySwaggerOperationsUrls(responseBody);

                    // Write the modified response body back to the original stream
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    context.HttpContext.Response.Body = originalBody;
                    context.HttpContext.Response.ContentLength = buffer.Length;
                    context.HttpContext.Response.ContentType = "application/json";
                    context.HttpContext.Response.Body.Write(buffer, 0, buffer.Length);
                }
            }
        });


        rewriteOptions.AddRedirect("^(|\\|\\s+)$", "/swagger"); // Regex for "/" and "" (whitespace)
        return rewriteOptions;
    }

    private static string ModifySwaggerOperationsUrls(string originalSwaggerJson)
    {
        // Here you can implement the logic to modify the Swagger operations' URLs
        // You can deserialize the Swagger JSON into objects, modify them, and then serialize back to JSON
        // For simplicity, let's assume we're replacing all occurrences of "http://localhost:5000" with "https://example.com"
        return originalSwaggerJson.Replace("http://localhost:5000", "https://example.com");
    }
}