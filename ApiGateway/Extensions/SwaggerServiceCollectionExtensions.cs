﻿using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Abstractions;
using AtiyanSeir.B2B.ApiGateway.Swagger;

namespace Microsoft.Extensions.DependencyInjection;

internal static class SwaggerServiceCollectionExtensions
{
    internal static void AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        //services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
    }

    /// <summary>
    /// Prepare central-swagger based on routes/clusters configurations
    /// </summary>
    /// <param name="app"></param>
    internal static void UseSwaggerIfNotProduction(this WebApplication app)
    {
        //if (app.Environment.IsProduction())

        var isCentralSwaggerEnabled = app.Configuration.GetValue<bool>("CentralSwagger:IsEnabled");
        if (!isCentralSwaggerEnabled)
        {
            app.Logger.LogDebug("Central swagger is disabled, so ignoring generating swagger documents");
            return;
        }

        app.Logger.LogDebug("Central swagger is enabled, so generating swagger documents based on routes/clusters...");
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "AtiyanSeir B2B API Gateway";

            var serviceDiscovery = app.Services.GetRequiredService<IServiceDiscovery>();
            var httpContext = app.Services.GetRequiredService<IHttpContextAccessor>();
            options.ConfigObject.Urls = new SwaggerEndpointEnumerator(serviceDiscovery, httpContext, app.Logger);
        });

        app.UseModifySwaggerOperationsUrlMiddleware();
    }
}
