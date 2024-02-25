using AtiyanSeir.B2B.ApiGateway.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
//builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.Request; });
builder.Services.AddHealthChecks();
builder.Services.AddConsulClient(builder.Configuration.GetSection("ConsulServiceDiscovery:ConsulClient"));
builder.Services.AddCustomSwagger();
builder.Services.AddCustomReverseProxy(builder.Configuration);

var app = builder.Build();

//app.UseHttpLogging();
app.UseRouting();
app.MapReverseProxy();
app.UseSwaggerIfNotProduction();
app.MapCustomEndpoints(builder);

app.Run();
