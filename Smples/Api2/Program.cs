using Api2;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var host = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Host") ?? throw new ArgumentException("Consul server address or not found!");
var datacenter = builder.Configuration.GetValue<string>("ConsulServiceDiscovery:ConsulClient:Datacenter") ?? string.Empty;
builder.Services.RegisterWithConsulServiceDiscovery(new AtiyanSeir.B2B.ApiGateway.ServiceDiscovery.Consul.ConsulServiceDiscoveryOptions.ConsulClientOptions(host, datacenter));

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