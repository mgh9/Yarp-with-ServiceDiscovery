using Microsoft.OpenApi;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Extensions;

namespace AtiyanSeir.B2B.ApiGateway.Extensions
{
    public class SwaggerBaseUrlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _yarpBaseUrl;

        public SwaggerBaseUrlMiddleware(RequestDelegate next, string yarpBaseUrl)
        {
            _next = next;
            _yarpBaseUrl = yarpBaseUrl;
        }

        //public async Task Invoke(HttpContext context)
        //{
        //    if (IsSwaggerPageRequest(context.Request))
        //    {
        //        // Modify operation URLs
        //        ModifyOperationUrls(context.Response);
        //    }

        //    // Continue processing the request pipeline
        //    await _next(context);
        //}

        public async Task Invoke(HttpContext context)
        {
            // Replace the response stream to intercept the content
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next delegate/middleware in the pipeline
            await _next(context);

            // Check if the response is a Swagger document
            if (context.Response.ContentType?.StartsWith("application/json") == true
                //context.Response.Headers.ContainsKey("Content-Disposition") &&
                //context.Response.Headers["Content-Disposition"] == "inline; filename=\"swagger.json\"")
                )
            {
                // Modify the Swagger document
                try
                {
                    await ModifySwaggerDocumentAsync(context, originalBodyStream);
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync("Errorrrrrrr : " + ex);
                }
            }
            else
            {
                // If the response is not a Swagger document, copy the original response body to the new stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private bool IsSwaggerPageRequest(HttpRequest request)
        {
            // Add logic to identify Swagger page requests
            // For example, check if the request path matches the Swagger UI endpoint
            return request.Path.StartsWithSegments("/swagger");
        }

        private void ModifyOperationUrls(HttpResponse response)
        {
            // Add logic to parse Swagger document and modify operation URLs
            // For example, update URLs by prefixing them with the YARP base URL
            // You can use libraries like Swashbuckle.AspNetCore to work with Swagger documents
        }

        private async Task ModifySwaggerDocumentAsync(HttpContext context, Stream originalBodyStream)
        {
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // Deserialize the Swagger document
            var swaggerDoc = await new OpenApiDocument.FromJsonAsync(responseBody);


            // Deserialize the Swagger document
            var reader = new OpenApiStringReader();
            var swaggerDoc = reader.Read(swaggerJson, out var diagnostic);

            // Modify operation URLs
            ModifyOperationUrls(swaggerDoc, _yarpBaseUrl);

            // Serialize the modified Swagger document back to JSON
            var newSwaggerJson = swaggerDoc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);

            // Write the modified Swagger document back to the response body
            var newResponseBody = Encoding.UTF8.GetBytes(newSwaggerJson);
            context.Response.Body.Write(newResponseBody, 0, newResponseBody.Length);
        }

        private void ModifyOperationUrls(OpenApiDocument swaggerDoc, string baseUrl)
        {
            foreach (var pathItem in swaggerDoc.Paths)
            {
                foreach (var operation in pathItem.Value.Operations)
                {
                    // Modify the URL to include the base URL
                    var ss = operation.Value.Extensions["x-operations-url"];// = $"{_yarpBaseUrl}{pathItem.Key}";
                }
            }
        }
    }

    public static class SwaggerBaseUrlMiddlewareExtensions
    {
        public static IApplicationBuilder UseSwaggerBaseUrlMiddleware(this IApplicationBuilder builder, string yarpBaseUrl)
        {
            return builder.UseMiddleware<SwaggerBaseUrlMiddleware>(yarpBaseUrl);
        }
    }

}
