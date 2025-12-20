using Infrastructure.Configuration;
using Infrastructure.Configuration.Api;
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
    public IActionResult GetFeatureFlag(string featureName)
    {
        try
        {
            var isEnabled = _featureFlagService.IsEnabled(featureName);
            var variant = _featureFlagService.GetVariant(featureName);
            return Ok(new { FeatureName = featureName, Enabled = isEnabled, Variant = variant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flag {FeatureName}", featureName);
            return StatusCode(500, new { Error = "Failed to retrieve feature flag" });
        }
    }

    [HttpGet]
    public IActionResult GetAllFeatureFlags()
    {
        try
        {
            var allFlags = _featureFlagService.GetAllFlags();
            return Ok(allFlags);
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
            _logger.LogError(ex, "Error setting feature flag {FeatureName}", flag.Name);
            return StatusCode(500, new { Error = "Failed to set feature flag" });
        }
    }
}

