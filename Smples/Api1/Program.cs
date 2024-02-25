using Api1;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Options;
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

ConsulClientOptions consulClientOptions = new();
builder.Configuration.GetSection("ConsulServiceRegistry:ConsulClient").Bind(consulClientOptions);
builder.Services.RegisterWithConsulServiceDiscovery(consulClientOptions);

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