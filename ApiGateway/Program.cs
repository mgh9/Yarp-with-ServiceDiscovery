var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddConsulClient(builder.Configuration.GetSection("ConsulServiceDiscovery:ConsulClient"));
builder.Services.AddCustomReverseProxy(builder.Configuration);
builder.Services.AddCustomSwagger();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();
app.UseCustomReverseProxy();
app.UseCustomSwagger();
app.MapCustomEndpoints(builder);

app.Run();
