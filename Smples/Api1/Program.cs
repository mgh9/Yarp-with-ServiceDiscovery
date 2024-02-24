using Api1;
using Coordinator.HealthChecking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
                    .AddCheck<MainHealthCheck>("Sample");
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opt => opt.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
{
    Title = "serviceeee1 Order titleeee",
    Description = "Description of the service 11 orderrr"
}));

var host = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Host") ?? throw new ArgumentException("Consul server address or not found!");
var datacenter = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Datacenter") ?? string.Empty;
builder.Services.RegisterWithConsulServiceDiscovery(new AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.ConsulServiceDiscoveryOptions.ConsulClientOptions(host, datacenter));

var app = builder.Build();

app.UseCors(builder => builder.AllowAnyOrigin());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(x =>
    {
        //x.SwaggerEndpoint("/swagger/v1/swagger.json", "MYYYY APP");
        x.DocumentTitle = "asgharrr";
    });
}

app.MapControllers();
app.MapHealthChecks("/healthz");
app.UseSampleEndpoints(builder.Environment);

app.Run();