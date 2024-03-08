using Api2;
using Yarp.ServiceDiscovery.Consul.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

ConsulServiceRegistryOptions serviceRegistryOptions = new();
builder.Configuration.GetSection("ConsulServiceRegistry").Bind(serviceRegistryOptions);
builder.Services.RegisterWithConsulServiceRegistry(serviceRegistryOptions);

var app = builder.Build();

app.UseCors(builder => builder.AllowAnyOrigin());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseSampleEndpoints(builder.Environment);

app.Run();