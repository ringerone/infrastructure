using Infrastructure.Configuration;
using Infrastructure.Configuration.Api.Models;
using Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Infrastructure.Configuration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        ITenantService tenantService,
        ILogger<TenantController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet("{tenantIdentifier}")]
    public async Task<IActionResult> GetTenant(string tenantIdentifier)
    {
        try
        {
            _logger.LogInformation("Getting tenant {TenantIdentifier}", tenantIdentifier);

            var tenant = await _tenantService.GetTenantAsync(tenantIdentifier);
            if (tenant == null)
            {
                return NotFound(new { Error = $"Tenant '{tenantIdentifier}' not found" });
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantIdentifier}", tenantIdentifier);
            return StatusCode(500, new { Error = "Failed to retrieve tenant" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTenants(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null)
    {
        try
        {
            _logger.LogInformation("Getting all tenants - Page: {PageNumber}, PageSize: {PageSize}, Search: {Search}, Status: {Status}",
                pageNumber, pageSize, search, status);

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (allTenants, totalCount) = await _tenantService.GetAllTenantsPagedAsync(
                pageNumber,
                pageSize,
                search,
                status);

            // Return paginated result
            var pagedResult = new Models.PagedResult<Tenant>
            {
                Items = allTenants,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _logger.LogInformation("Retrieved {Count} tenants (Page {PageNumber} of {TotalPages})",
                allTenants.Count(), pageNumber, pagedResult.TotalPages);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants");
            return StatusCode(500, new { Error = "Failed to retrieve tenants" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetTenant([FromBody] Tenant tenant, CancellationToken cancellationToken)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(tenant.TenantIdentifier))
            {
                return BadRequest(new { Error = "Tenant Identifier is required." });
            }
            if (string.IsNullOrWhiteSpace(tenant.Name))
            {
                return BadRequest(new { Error = "Tenant Name is required." });
            }
            if (!Enum.IsDefined(typeof(TenantStatus), tenant.Status))
            {
                return BadRequest(new { Error = $"Invalid Tenant Status: {tenant.Status}. Valid values are: Active, Inactive, Pending, Suspended." });
            }

            // Validate tenant identifier format (alphanumeric, hyphens, underscores)
            if (!System.Text.RegularExpressions.Regex.IsMatch(tenant.TenantIdentifier, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest(new { Error = "Tenant Identifier can only contain letters, numbers, hyphens, and underscores." });
            }

            await _tenantService.SetTenantAsync(tenant, cancellationToken);

            return Ok(new { Message = "Tenant saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tenant {TenantIdentifier}", tenant?.TenantIdentifier);
            return StatusCode(500, new { Error = "Failed to save tenant" });
        }
    }

    [HttpDelete("{tenantIdentifier}")]
    public async Task<IActionResult> DeleteTenant(string tenantIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting tenant {TenantIdentifier}", tenantIdentifier);

            await _tenantService.DeleteTenantAsync(tenantIdentifier, cancellationToken);

            return Ok(new { Message = "Tenant deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantIdentifier}", tenantIdentifier);
            return StatusCode(500, new { Error = "Failed to delete tenant" });
        }
    }
}

