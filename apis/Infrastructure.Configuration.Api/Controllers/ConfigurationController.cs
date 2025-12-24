using Infrastructure.Configuration;
using Infrastructure.Configuration.Api;
using Infrastructure.Configuration.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Infrastructure.Configuration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IConfigurationNotificationService _notificationService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IConfigurationService configurationService,
        IConfigurationNotificationService notificationService,
        ILogger<ConfigurationController> logger)
    {
        _configurationService = configurationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetConfiguration(string key, [FromQuery] ConfigurationScope? scope, [FromQuery] string? scopeIdentifier)
    {
        try
        {
            _logger.LogInformation("Getting configuration: {Key}", key);
            
            // Get the configuration from repository with full information
            var repository = HttpContext.RequestServices.GetRequiredService<Infrastructure.Configuration.IConfigurationRepository>();
            var tenantAccessor = HttpContext.RequestServices.GetRequiredService<Infrastructure.MultiTenancy.ITenantContextAccessor>();
            var environment = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var region = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Region"];
            
            // Try to find the configuration by key across all scopes
            var scopes = new[]
            {
                Infrastructure.Configuration.ConfigurationScope.User,
                Infrastructure.Configuration.ConfigurationScope.Tenant,
                Infrastructure.Configuration.ConfigurationScope.Region,
                Infrastructure.Configuration.ConfigurationScope.Environment,
                Infrastructure.Configuration.ConfigurationScope.Global
            };
            
            Infrastructure.Configuration.ConfigurationEntry? foundEntry = null;
            foreach (var searchScope in scopes)
            {
                string? searchScopeIdentifier = searchScope switch
                {
                    Infrastructure.Configuration.ConfigurationScope.Environment => environment.EnvironmentName,
                    Infrastructure.Configuration.ConfigurationScope.Region => region,
                    Infrastructure.Configuration.ConfigurationScope.Tenant => tenantAccessor.CurrentTenant?.TenantId,
                    _ => null
                };
                
                foundEntry = await repository.GetConfigurationAsync(key, searchScope, searchScopeIdentifier);
                if (foundEntry != null)
                    break;
            }
            
            // If not found by scope, try to find any configuration with this key (for editing)
            if (foundEntry == null)
            {
                var allConfigs = await repository.GetAllConfigurationsAsync(new Dictionary<Infrastructure.Configuration.ConfigurationScope, string?>());
                foundEntry = allConfigs.FirstOrDefault(c => c.Key == key);
            }
            
            if (foundEntry == null)
            {
                return NotFound(new { Error = $"Configuration '{key}' not found" });
            }
            
            return Ok(new
            {
                key = foundEntry.Key,
                value = foundEntry.Value,
                scope = foundEntry.Scope.ToString(),
                scopeIdentifier = foundEntry.ScopeIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration {Key}", key);
            return StatusCode(500, new { Error = "Failed to retrieve configuration" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllConfigurations(
        [FromQuery] ConfigurationScope? scope,
        [FromQuery] string? scopeIdentifier,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            _logger.LogInformation("Getting configurations - Scope: {Scope}, ScopeIdentifier: {ScopeIdentifier}, Page: {PageNumber}, PageSize: {PageSize}, Search: {Search}", 
                scope, scopeIdentifier, pageNumber, pageSize, search);
            
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            
            // Get all configuration entries with scope information
            var repository = HttpContext.RequestServices.GetRequiredService<Infrastructure.Configuration.IConfigurationRepository>();
            var tenantAccessor = HttpContext.RequestServices.GetRequiredService<Infrastructure.MultiTenancy.ITenantContextAccessor>();
            var environment = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var region = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Region"];
            
            var scopeIdentifiers = new Dictionary<Infrastructure.Configuration.ConfigurationScope, string?>
            {
                [Infrastructure.Configuration.ConfigurationScope.Global] = null,
                [Infrastructure.Configuration.ConfigurationScope.Environment] = environment.EnvironmentName,
                [Infrastructure.Configuration.ConfigurationScope.Region] = region,
                [Infrastructure.Configuration.ConfigurationScope.Tenant] = tenantAccessor.CurrentTenant?.TenantId,
                [Infrastructure.Configuration.ConfigurationScope.User] = null
            };
            
            // Get paginated results with filters applied at database level
            var (allEntries, totalCount) = await repository.GetAllConfigurationsPagedAsync(
                scopeIdentifiers,
                pageNumber,
                pageSize,
                search,
                scope,
                scopeIdentifier);
            
            // Return as dictionary with key -> { value, scope, scopeIdentifier }
            var result = new Dictionary<string, object>();
            foreach (var entry in allEntries)
            {
                if (!result.ContainsKey(entry.Key))
                {
                    result[entry.Key] = new
                    {
                        value = entry.Value,
                        scope = entry.Scope.ToString(),
                        scopeIdentifier = entry.ScopeIdentifier
                    };
                }
            }
            
            // Return paginated result
            var pagedResult = new Models.PagedResult<object>
            {
                Items = result.Select(kvp => new { key = kvp.Key, value = kvp.Value }),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            
            _logger.LogInformation("Retrieved {Count} configurations (Page {PageNumber} of {TotalPages})", 
                totalCount, pageNumber, pagedResult.TotalPages);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configurations");
            return StatusCode(500, new { Error = "Failed to retrieve configurations" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetConfiguration([FromBody] SetConfigurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest(new { Error = "Key is required" });
            }

            if (request.Value == null)
            {
                return BadRequest(new { Error = "Value is required" });
            }

            // Validate scope
            if (!Enum.IsDefined(typeof(ConfigurationScope), request.Scope))
            {
                return BadRequest(new { Error = "Invalid scope value" });
            }

            // Validate scope identifier for Global scope
            if (request.Scope == ConfigurationScope.Global && !string.IsNullOrEmpty(request.ScopeIdentifier))
            {
                _logger.LogWarning("Global scope should not have a scope identifier. Ignoring scope identifier.");
                request.ScopeIdentifier = null;
            }

            _logger.LogInformation("Received SetConfiguration request: Key={Key}, Scope={Scope}, ScopeIdentifier={ScopeIdentifier}", 
                request.Key, request.Scope, request.ScopeIdentifier);

            // Convert JsonElement to actual value type for MongoDB serialization
            object value = request.Value;
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                value = ConfigurationControllerHelpers.ConvertJsonElement(jsonElement);
            }

            await _configurationService.SetValueAsync(
                request.Key,
                value,
                request.Scope,
                request.ScopeIdentifier,
                cancellationToken);

            // Notify clients of configuration change
            await _notificationService.NotifyConfigurationChangedAsync(
                request.Key,
                request.Scope,
                request.ScopeIdentifier,
                cancellationToken);

            return Ok(new { Message = "Configuration set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Key}", request.Key);
            return StatusCode(500, new { Error = "Failed to set configuration" });
        }
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteConfiguration(string key, [FromQuery] ConfigurationScope scope, [FromQuery] string? scopeIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            // Implementation would require adding DeleteAsync to IConfigurationService
            // For now, return not implemented
            return StatusCode(501, new { Error = "Delete not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Key}", key);
            return StatusCode(500, new { Error = "Failed to delete configuration" });
        }
    }
}

public class SetConfigurationRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public object Value { get; set; } = default!;
    
    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    [System.Text.Json.Serialization.JsonConverter(typeof(ConfigurationScopeJsonConverter))]
    public ConfigurationScope Scope { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("scopeIdentifier")]
    public string? ScopeIdentifier { get; set; }
}

/// <summary>
/// Custom JSON converter for ConfigurationScope that properly handles string values
/// </summary>
public class ConfigurationScopeJsonConverter : System.Text.Json.Serialization.JsonConverter<ConfigurationScope>
{
    public override ConfigurationScope Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (Enum.TryParse<ConfigurationScope>(stringValue, ignoreCase: false, out var result))
            {
                return result;
            }
            // If case-insensitive parse fails, try case-insensitive
            if (Enum.TryParse<ConfigurationScope>(stringValue, ignoreCase: true, out result))
            {
                return result;
            }
            throw new System.Text.Json.JsonException($"Invalid ConfigurationScope value: '{stringValue}'. Valid values are: Global, Environment, Region, Tenant, User");
        }
        else if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
        {
            var intValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(ConfigurationScope), intValue))
            {
                return (ConfigurationScope)intValue;
            }
            throw new System.Text.Json.JsonException($"Invalid ConfigurationScope numeric value: {intValue}");
        }
        
        throw new System.Text.Json.JsonException($"Unexpected token type for ConfigurationScope: {reader.TokenType}. Expected String or Number.");
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, ConfigurationScope value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Helper methods for configuration controller
/// </summary>
public static class ConfigurationControllerHelpers
{
    /// <summary>
    /// Converts JsonElement to a MongoDB-serializable value
    /// </summary>
    public static object ConvertJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString()!,
            System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null!,
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            System.Text.Json.JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                prop => prop.Name,
                prop => ConvertJsonElement(prop.Value)),
            _ => element.GetRawText()
        };
    }
}

