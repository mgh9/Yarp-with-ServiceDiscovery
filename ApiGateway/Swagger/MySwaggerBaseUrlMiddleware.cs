using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using Newtonsoft.Json.Linq;
//    using Yarp.ReverseProxy.Swagger;

namespace AtiyanSeir.B2B.ApiGateway.Swagger
{

    public class MySwaggerBaseUrlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceDiscovery _serviceDiscovery;

        //private readonly IOptionsMonitor<ReverseProxyDocumentFilterConfig> _config;

        public MySwaggerBaseUrlMiddleware(RequestDelegate next,IServiceDiscovery serviceDiscovery)//, IOptionsMonitor<ReverseProxyDocumentFilterConfig> config)
        {
            _next = next;
            this._serviceDiscovery = serviceDiscovery;
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

        private List<List<string>> F1(JArray array)
        {
            List<List<string>> result = new();

            foreach (JObject pathObj in array.SelectTokens("paths").AsJEnumerable())
            {
                Dictionary<string, JObject> pathDict = pathObj.ToObject<Dictionary<string, JObject>>();

                foreach (KeyValuePair<string, JObject> pathKvp in pathDict)
                {
                    var path = pathKvp.Key;
                    var methodDict = pathKvp.Value.ToObject<Dictionary<string, JObject>>();

                    foreach (KeyValuePair<string, JObject> methodKvp in methodDict)
                    {
                        string method = methodKvp.Key;
                        string summary = (string)methodKvp.Value.SelectToken("summary");
                        string tag = (string)methodKvp.Value.SelectToken("tags").AsEnumerable().First();

                        result.Add(new List<string> { summary, tag, path, method });
                    }
                }
            }

            return result;
        }

        private static bool IsSwaggerUi(PathString pathString)
        {
            var s=  pathString.ToUriComponent().EndsWith("swagger.json", StringComparison.OrdinalIgnoreCase);
            return s;
            //return pathString.StartsWithSegments("/swagger");
        }

        public async Task Invoke(HttpContext context)
        {
            if (!IsSwaggerUi(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var originBody = context.Response.Body;

            try
            {
                var routes = _serviceDiscovery.GetRoutes();
                var clusters = _serviceDiscovery.GetClusters();
                var conf = _serviceDiscovery.ExportConfigs();

                var memStream = new MemoryStream();
                context.Response.Body = memStream;

                await _next(context).ConfigureAwait(false);

                memStream.Position = 0;
                var responseBody = new StreamReader(memStream).ReadToEnd();

                //https://stackoverflow.com/questions/44508028/modify-middleware-response
                var arr = context.Request.Path.Value.Split('/');
                var clusterName = arr[2]!;
                var c = clusters.SingleOrDefault(x => x.ClusterId == clusterName);

                var routeId = $"{clusterName}-route"!;
                var mainRoute = routes.SingleOrDefault(x => x.RouteId == routeId);
                var routePathWithoutReminder = mainRoute.Match.Path.Replace("/{**remainder}", "");


                JObject jObject = JObject.Parse(responseBody);
                var apiName = jObject["info"]["title"].ToString();

                // Get the paths object
                JObject paths = (JObject)jObject["paths"];

                // Create a new paths object
                JObject newPaths = new JObject();

                foreach (JProperty property in paths.Properties())
                {
                    string newName = $"{routePathWithoutReminder}{property.Name}";

                    newPaths.Add(newName, property.Value);
                }

                jObject["paths"] = newPaths;
                string modifiedJson = jObject.ToString();

                Console.WriteLine(modifiedJson);

                responseBody = modifiedJson;



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
