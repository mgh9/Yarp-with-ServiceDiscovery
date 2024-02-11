using Consul;
using Coordinator.HealthChecking;
using MyShared;
using Serilog;
using Serilog.Events;

namespace Api1;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);
        AddSerilogConfiguration(builder);

        // Add services to the container.
        //builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IConsulClient, ConsulClient>(c => new ConsulClient(consulConfig =>
        {
            consulConfig.Address = new Uri(builder.Configuration["ServiceDiscovery:Consul:Address"]);
        }));

        //builder.Services.AddSingleton<IServiceDiscoveryKeyValueProvider, ServiceDiscoveryKeyValueProvider>();
        //builder.Services.Configure<ServiceDiscoveryOptions>(builder.Configuration.GetSection("ServiceDiscovery"));

        builder.Services.AddControllers();

        builder.Services.AddHealthChecks()
                            .AddCheck<MainHealthCheck>("Sample");

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            // AllowCachingResponses = true,
        });

        app.UseAuthorization();
        app.MapControllers();

        var serviceDiscoveryOptions = app.Configuration.GetSection("ServiceDiscovery").Get<ServiceDiscoveryOptions>();
        app.AddConsulServiceDiscovery(serviceDiscoveryOptions, app.Lifetime);

        app.Run();
    }

    static void AddSerilogConfiguration(WebApplicationBuilder builder)
    {
        // NOTE: In .Net 6 the UseSerilog method now shows up on the Host;
        // in .Net Core 3 it was on the Builder.
        builder.Host.UseSerilog((hostContext, services, loggerConfiguration) =>
        {
            // Use the Serilog configuration defined in the application config. e.g. the appsettings.json file.
            loggerConfiguration
                .ReadFrom.Configuration(hostContext.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });
    }
}
