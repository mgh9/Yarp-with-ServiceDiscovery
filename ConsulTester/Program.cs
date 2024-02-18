using Coordinator.HealthChecking;
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

        builder.Services.RegisterWithConsulServiceDiscovery(builder.Configuration.GetSection("ConsulServiceDiscovery"));

        builder.Services.AddControllers();

        builder.Services.AddHealthChecks()
                            .AddCheck<MainHealthCheck>("Sample");

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors();

        var app = builder.Build();

        app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

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
