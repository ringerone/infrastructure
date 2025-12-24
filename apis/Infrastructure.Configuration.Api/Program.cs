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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Configure enum converter to handle string values (case-insensitive, exact enum name matching)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(
            namingPolicy: null, // Use exact enum name matching (Global, Environment, etc.)
            allowIntegerValues: false));
    });
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

// Get OTLP endpoint from configuration or use default
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] 
    ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] 
    ?? "http://otel-collector:4317";

LoggingBootstrap.ConfigureOpenTelemetry(builder.Services, new LoggingOptions
{
    ServiceName = "Configuration.Api",
    ServiceVersion = "1.0.0",
    Environment = builder.Environment.EnvironmentName,
    EnableMultiTenancy = true,
    EnableConsoleExporter = true,
    OtlpExporterOptions = new OtlpExporterOptions
    {
        Endpoint = new Uri(otlpEndpoint),
        Headers = builder.Configuration["OpenTelemetry:OtlpHeaders"]
    }
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

builder.Services.AddScoped<ITenantRepository>(sp =>
{
    var factory = sp.GetRequiredService<IDataAccessFactory>();
    return new MongoDbTenantRepository(factory);
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

builder.Services.AddScoped<ITenantService>(sp =>
{
    var repository = sp.GetRequiredService<ITenantRepository>();
    return new TenantService(repository);
});

// Add SignalR for WebSocket pub/sub
builder.Services.AddSignalR();
builder.Services.AddSingleton<IConfigurationNotificationService, ConfigurationNotificationService>();

// Configure CORS to allow Angular app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS (must be before UseAuthorization and after UseRouting/UseHttpsRedirection)
app.UseCors("AllowAngularApp");

// Add tenant middleware early in pipeline
app.UseTenantContext();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ConfigurationHub>("/hubs/configuration");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

