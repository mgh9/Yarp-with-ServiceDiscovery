var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
//builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.Request; });
builder.Services.AddHealthChecks();
builder.Services.AddConsulClient(builder.Configuration.GetSection("ConsulServiceDiscovery:ConsulClient"));
builder.Services.AddCustomSwagger();
builder.Services.AddCustomReverseProxy(builder.Configuration);

var app = builder.Build();

app.Logger.LogInformation("Environment is {envName}", app.Environment.EnvironmentName);

//app.UseHttpLogging();
app.UseRouting();
app.UseCustomReverseProxy();
app.UseCustomSwagger();
app.MapCustomEndpoints(builder);

app.Run();
