using Infrastructure.Configuration;
using Infrastructure.Configuration.Api;
using Infrastructure.Configuration.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Configuration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureFlagController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IConfigurationNotificationService _notificationService;
    private readonly ILogger<FeatureFlagController> _logger;

    public FeatureFlagController(
        IFeatureFlagService featureFlagService,
        IConfigurationNotificationService notificationService,
        ILogger<FeatureFlagController> logger)
    {
        _featureFlagService = featureFlagService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet("{featureName}")]
    public async Task<IActionResult> GetFeatureFlag(string featureName)
    {
        try
        {
            _logger.LogInformation("Getting feature flag: {FeatureName}", featureName);
            
            // Get the feature flag from repository with full information
            var repository = HttpContext.RequestServices.GetRequiredService<Infrastructure.Configuration.IFeatureFlagRepository>();
            var tenantAccessor = HttpContext.RequestServices.GetRequiredService<Infrastructure.MultiTenancy.ITenantContextAccessor>();
            var environment = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var region = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Region"];
            
            // Try to find the feature flag by name across all scopes
            var scopes = new[]
            {
                Infrastructure.Configuration.ConfigurationScope.User,
                Infrastructure.Configuration.ConfigurationScope.Tenant,
                Infrastructure.Configuration.ConfigurationScope.Region,
                Infrastructure.Configuration.ConfigurationScope.Environment,
                Infrastructure.Configuration.ConfigurationScope.Global
            };
            
            FeatureFlag? foundFlag = null;
            foreach (var scope in scopes)
            {
                string? scopeIdentifier = scope switch
                {
                    Infrastructure.Configuration.ConfigurationScope.Environment => environment.EnvironmentName,
                    Infrastructure.Configuration.ConfigurationScope.Region => region,
                    Infrastructure.Configuration.ConfigurationScope.Tenant => tenantAccessor.CurrentTenant?.TenantId,
                    _ => null
                };
                
                foundFlag = await repository.GetFeatureFlagAsync(featureName, scope, scopeIdentifier);
                if (foundFlag != null)
                    break;
            }
            
            // If not found by scope, try to find any flag with this name (for editing)
            if (foundFlag == null)
            {
                var allFlags = await repository.GetAllFeatureFlagsAsync(new Dictionary<Infrastructure.Configuration.ConfigurationScope, string?>());
                foundFlag = allFlags.FirstOrDefault(f => f.Name == featureName);
            }
            
            if (foundFlag == null)
            {
                return NotFound(new { Error = $"Feature flag '{featureName}' not found" });
            }
            
            return Ok(new
            {
                featureName = foundFlag.Name,
                enabled = foundFlag.Enabled,
                variant = foundFlag.Variant,
                scope = foundFlag.Scope.ToString(),
                scopeIdentifier = foundFlag.ScopeIdentifier,
                rolloutPercentage = foundFlag.RolloutPercentage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flag {FeatureName}", featureName);
            return StatusCode(500, new { Error = "Failed to retrieve feature flag" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFeatureFlags(
        [FromQuery] ConfigurationScope? scope,
        [FromQuery] string? scopeIdentifier,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            _logger.LogInformation("Getting feature flags - Scope: {Scope}, ScopeIdentifier: {ScopeIdentifier}, Page: {PageNumber}, PageSize: {PageSize}, Search: {Search}", 
                scope, scopeIdentifier, pageNumber, pageSize, search);
            
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            
            // Get all feature flag entries with full information
            var repository = HttpContext.RequestServices.GetRequiredService<Infrastructure.Configuration.IFeatureFlagRepository>();
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
            var (allEntries, totalCount) = await repository.GetAllFeatureFlagsPagedAsync(
                scopeIdentifiers,
                pageNumber,
                pageSize,
                search,
                scope,
                scopeIdentifier);
            
            // Return as dictionary with name -> { enabled, variant, rolloutPercentage, scope, scopeIdentifier }
            var result = new Dictionary<string, object>();
            foreach (var entry in allEntries)
            {
                if (!result.ContainsKey(entry.Name))
                {
                    result[entry.Name] = new
                    {
                        enabled = entry.Enabled,
                        variant = entry.Variant,
                        rolloutPercentage = entry.RolloutPercentage,
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
            
            _logger.LogInformation("Retrieved {Count} feature flags (Page {PageNumber} of {TotalPages})", 
                totalCount, pageNumber, pagedResult.TotalPages);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all feature flags");
            return StatusCode(500, new { Error = "Failed to retrieve feature flags" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetFeatureFlag([FromBody] FeatureFlag flag, CancellationToken cancellationToken)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(flag.Name))
            {
                return BadRequest(new { Error = "Name is required" });
            }

            // Validate scope
            if (!Enum.IsDefined(typeof(ConfigurationScope), flag.Scope))
            {
                return BadRequest(new { Error = "Invalid scope value" });
            }

            // Validate rollout percentage
            if (flag.RolloutPercentage < 0 || flag.RolloutPercentage > 100)
            {
                return BadRequest(new { Error = "Rollout percentage must be between 0 and 100" });
            }

            // Validate scope identifier for Global scope
            if (flag.Scope == ConfigurationScope.Global && !string.IsNullOrEmpty(flag.ScopeIdentifier))
            {
                _logger.LogWarning("Global scope should not have a scope identifier. Ignoring scope identifier.");
                flag.ScopeIdentifier = null;
            }

            _logger.LogInformation("Received SetFeatureFlag request: Name={Name}, Enabled={Enabled}, Scope={Scope}, ScopeIdentifier={ScopeIdentifier}, RolloutPercentage={RolloutPercentage}", 
                flag.Name, flag.Enabled, flag.Scope, flag.ScopeIdentifier, flag.RolloutPercentage);

            await _featureFlagService.SetFeatureFlagAsync(flag, cancellationToken);

            // Notify clients of feature flag change
            await _notificationService.NotifyFeatureFlagChangedAsync(
                flag.Name,
                flag.Scope,
                flag.ScopeIdentifier,
                cancellationToken);

            return Ok(new { Message = "Feature flag set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting feature flag {FeatureName}", flag?.Name);
            return StatusCode(500, new { Error = "Failed to set feature flag" });
        }
    }
}

