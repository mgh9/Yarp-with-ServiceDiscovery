//namespace AtiyanSeir.B2B.ApiGateway.Swagger
//{
//    using Microsoft.AspNetCore.Http;
//    using Microsoft.Extensions.Options;
//    using System;
//    using System.Threading.Tasks;
////    using Yarp.ReverseProxy.Swagger;

//    public class MySwaggerBaseUrlMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly IOptionsMonitor<ReverseProxyDocumentFilterConfig> _config;

//        public MySwaggerBaseUrlMiddleware(RequestDelegate next, IOptionsMonitor<ReverseProxyDocumentFilterConfig> config)
//        {
//            _next = next;
//            _config = config;
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            if (context.Request.Path.StartsWithSegments("/swagger"))
//            {
//                // Retrieve the current request scheme and host
//                var requestScheme = context.Request.Scheme;
//                var requestHost = context.Request.Host;

//                // Dynamically determine the base URL based on the request
//                var baseUrl = $"{requestScheme}://{requestHost}/proxy";

//                // Modify the base URL for Swagger operations
//                var config = _config.CurrentValue;
//                foreach (var operation in config.Routes)
//                {
//                    operation.Value.Endpoint.HttpMethod = baseUrl + operation.Value.Endpoint.HttpMethod;
//                }
//            }

//            await _next(context);
//        }
//    }
//}
