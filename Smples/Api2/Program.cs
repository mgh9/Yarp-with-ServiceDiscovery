using Api2;
using AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

ConsulClientOptions consulClientOptions = new();
builder.Configuration.GetSection("ConsulServiceRegistry:ConsulClient").Bind(consulClientOptions);
builder.Services.RegisterWithConsulServiceDiscovery(consulClientOptions);

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