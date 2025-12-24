using Infrastructure.Configuration;
using Infrastructure.Configuration.Api;
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
            if (scope.HasValue)
            {
                var value = _configurationService.GetValue<object>(key, scope.Value);
                return Ok(new { Key = key, Value = value, Scope = scope.Value });
            }
            else
            {
                var value = _configurationService.GetValue<object>(key);
                return Ok(new { Key = key, Value = value });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration {Key}", key);
            return StatusCode(500, new { Error = "Failed to retrieve configuration" });
        }
    }

    [HttpGet]
    public IActionResult GetAllConfigurations()
    {
        try
        {
            _logger.LogInformation("Getting all configurations");
            var allValues = _configurationService.GetAllValues();
            _logger.LogInformation("Retrieved {Count} configurations", allValues.Count);
            return Ok(allValues);
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
    public ConfigurationScope Scope { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("scopeIdentifier")]
    public string? ScopeIdentifier { get; set; }
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

