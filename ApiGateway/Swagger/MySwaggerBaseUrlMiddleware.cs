using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
//    using Yarp.ReverseProxy.Swagger;

namespace AtiyanSeir.B2B.ApiGateway.Swagger
{

    public class MySwaggerBaseUrlMiddleware
    {
        private readonly RequestDelegate _next;
        //private readonly IOptionsMonitor<ReverseProxyDocumentFilterConfig> _config;

        public MySwaggerBaseUrlMiddleware(RequestDelegate next)//, IOptionsMonitor<ReverseProxyDocumentFilterConfig> config)
        {
            _next = next;
            //     _config = config;
        }

        //public async Task InvokeAsync(HttpContext context)
        //{
        //    var originalBodyStream = context.Response.Body;

        //    try
        //    {
        //        using (var memStream = new MemoryStream())
        //        {
        //            context.Response.Body = memStream;

        //            await _next(context);

        //            memStream.Position = 0;

        //            string responseBody = new StreamReader(memStream).ReadToEnd();


        //            memStream.Position = 0;
        //            await memStream.CopyToAsync(originalBodyStream);
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        //throw;
        //    }
        //    finally
        //    {
        //        context.Response.Body = originalBodyStream;
        //    }
        //}

        public async Task Invoke(HttpContext context)
        {
            var originBody = context.Response.Body;
            try
            {
                var memStream = new MemoryStream();
                context.Response.Body = memStream;

                await _next(context).ConfigureAwait(false);

                memStream.Position = 0;
                var responseBody = new StreamReader(memStream).ReadToEnd();

            //https://stackoverflow.com/questions/44508028/modify-middleware-response
sample// api01 ro modify kard okeye...automate kon

                //Custom logic to modify response
                responseBody = responseBody.Replace("/GetApi", "/api01/GetApi", StringComparison.InvariantCultureIgnoreCase);

                var memoryStreamModified = new MemoryStream();
                var sw = new StreamWriter(memoryStreamModified);
                sw.Write(responseBody);
                sw.Flush();
                memoryStreamModified.Position = 0;

                await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
            }
            finally
            {
                context.Response.Body = originBody;
            }
        }

        private async Task ModifyResponseAsync(HttpResponse response)
        {
            var stream = response.Body;
            using var reader = new StreamReader(stream, leaveOpen: true);

            string responseBody = reader.ReadToEnd();

            string originalResponse = await reader.ReadToEndAsync();
            string modifiedResponse = "Hello from Stackoverflow";

            stream.SetLength(0);
            using var writer = new StreamWriter(stream, leaveOpen: true);
            await writer.WriteAsync(modifiedResponse);
            await writer.FlushAsync();
            response.ContentLength = stream.Length;
        }

        private string ModifySwaggerDocument(string originalSwagger)
        {
            // Modify the original Swagger document as needed
            // For example, you can use JSON.NET to deserialize the document, make modifications, and then serialize it back to JSON
            // Example:
            // var swagger = JsonConvert.DeserializeObject<SwaggerDocument>(originalSwagger);
            // Modify the Swagger document here
            // var modifiedSwagger = ModifySwagger(swagger);
            // return JsonConvert.SerializeObject(modifiedSwagger);

            return originalSwagger;
        }

        private string ModifySwaggerJson(string originalJson)
        {
            // Deserialize the Swagger JSON into a JObject
            var swagger = JObject.Parse(originalJson);

            // Modify the paths in the Swagger JSON
            foreach (var path in swagger["paths"].Children<JProperty>())
            {
                // Modify the path here
                var newPath = "/api" + path.Name;
                path.Replace(new JProperty(newPath, path.Value));
            }

            // Serialize the JObject back into a string
            var modifiedJson = swagger.ToString();

            return modifiedJson;
        }
    }

    public static class ModifySwaggerResponseMiddlewareExtensions
    {
        public static IApplicationBuilder UseModifySwaggerResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MySwaggerBaseUrlMiddleware>();
        }
    }
}
