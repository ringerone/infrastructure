using Infrastructure.Configuration;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Configuration.Api;

using Infrastructure.Configuration;

/// <summary>
/// SignalR hub for configuration change notifications
/// Implements pub/sub pattern for configuration refresh
/// </summary>
public class ConfigurationHub : Hub
{
    private readonly IConfigurationService _configurationService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<ConfigurationHub> _logger;

    public ConfigurationHub(
        IConfigurationService configurationService,
        IFeatureFlagService featureFlagService,
        ILogger<ConfigurationHub> logger)
    {
        _configurationService = configurationService;
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.GetHttpContext()?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
            _logger.LogInformation("Client {ConnectionId} connected for tenant {TenantId}", Context.ConnectionId, tenantId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Service for broadcasting configuration changes via SignalR
/// </summary>
public interface IConfigurationNotificationService
{
    Task NotifyConfigurationChangedAsync(string key, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default);
    Task NotifyFeatureFlagChangedAsync(string featureName, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default);
}

public class ConfigurationNotificationService : IConfigurationNotificationService
{
    private readonly IHubContext<ConfigurationHub> _hubContext;
    private readonly ILogger<ConfigurationNotificationService> _logger;

    public ConfigurationNotificationService(
        IHubContext<ConfigurationHub> hubContext,
        ILogger<ConfigurationNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyConfigurationChangedAsync(string key, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = scopeIdentifier != null ? $"tenant:{scopeIdentifier}" : "global";
            await _hubContext.Clients.Group(groupName).SendAsync("ConfigurationChanged", new
            {
                Key = key,
                Scope = scope.ToString(),
                ScopeIdentifier = scopeIdentifier,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
            
            _logger.LogInformation("Configuration change notification sent for {Key} at scope {Scope}", key, scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send configuration change notification for {Key}", key);
        }
    }

    public async Task NotifyFeatureFlagChangedAsync(string featureName, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = scopeIdentifier != null ? $"tenant:{scopeIdentifier}" : "global";
            await _hubContext.Clients.Group(groupName).SendAsync("FeatureFlagChanged", new
            {
                FeatureName = featureName,
                Scope = scope.ToString(),
                ScopeIdentifier = scopeIdentifier,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
            
            _logger.LogInformation("Feature flag change notification sent for {FeatureName} at scope {Scope}", featureName, scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send feature flag change notification for {FeatureName}", featureName);
        }
    }
}

