using Infrastructure.Configuration;
using Infrastructure.Configuration.Api;
using Infrastructure.Configuration.Database;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.MongoDB;
using Infrastructure.Logging;
using Infrastructure.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure multi-tenancy
builder.Services.AddMultiTenancy();

// Configure MongoDB data access
builder.Services.AddMongoDbDataAccess(options =>
{
    options.DefaultConnectionString = builder.Configuration["MongoDb:ConnectionString"];
    options.DefaultDatabaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "ConfigurationDb";
});

// Configure logging and telemetry
builder.Services.AddMemoryCache();
LoggingBootstrap.ConfigureOpenTelemetry(builder.Services, new LoggingOptions
{
    ServiceName = "Configuration.Api",
    ServiceVersion = "1.0.0",
    Environment = builder.Environment.EnvironmentName,
    EnableMultiTenancy = true,
    EnableConsoleExporter = true
});

// Register configuration services
var environment = builder.Environment.EnvironmentName;
var region = builder.Configuration["Region"];

builder.Services.AddScoped<IConfigurationRepository>(sp =>
{
    var factory = sp.GetRequiredService<IDataAccessFactory>();
    return new MongoDbConfigurationRepository(factory);
});

builder.Services.AddScoped<IFeatureFlagRepository>(sp =>
{
    var factory = sp.GetRequiredService<IDataAccessFactory>();
    return new MongoDbFeatureFlagRepository(factory);
});

builder.Services.AddScoped<IConfigurationService>(sp =>
{
    var repository = sp.GetRequiredService<IConfigurationRepository>();
    var tenantAccessor = sp.GetRequiredService<ITenantContextAccessor>();
    var logger = sp.GetRequiredService<ILogger<ConfigurationService>>();
    var activityFactory = sp.GetRequiredService<Infrastructure.Telemetry.IActivitySourceFactory>();
    var memoryCache = sp.GetService<IMemoryCache>();
    
    return new ConfigurationService(
        repository,
        tenantAccessor,
        logger,
        activityFactory,
        environment,
        region,
        memoryCache,
        TimeSpan.FromMinutes(5));
});

builder.Services.AddScoped<IFeatureFlagService>(sp =>
{
    var repository = sp.GetRequiredService<IFeatureFlagRepository>();
    var tenantAccessor = sp.GetRequiredService<ITenantContextAccessor>();
    var logger = sp.GetRequiredService<ILogger<FeatureFlagService>>();
    var activityFactory = sp.GetRequiredService<Infrastructure.Telemetry.IActivitySourceFactory>();
    var memoryCache = sp.GetService<IMemoryCache>();
    
    return new FeatureFlagService(
        repository,
        tenantAccessor,
        logger,
        activityFactory,
        environment,
        region,
        memoryCache,
        TimeSpan.FromMinutes(5));
});

// Add SignalR for WebSocket pub/sub
builder.Services.AddSignalR();
builder.Services.AddSingleton<IConfigurationNotificationService, ConfigurationNotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add tenant middleware early in pipeline
app.UseTenantContext();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ConfigurationHub>("/hubs/configuration");

app.Run();

