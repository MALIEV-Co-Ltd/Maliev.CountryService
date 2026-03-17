using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Application.Models.Countries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.CountryService.Api.Controllers;

/// <summary>
/// Administrative endpoints for managing country data (CRUD operations).
/// Requires authentication and role-based authorization. Most operations require CountryAdmin role, hard delete requires SuperAdmin.
/// All modifications are logged with full audit trail including user context and ETag-based optimistic concurrency control.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("country/v{version:apiVersion}/admin/countries")]
[EnableRateLimiting(RateLimitPolicies.Admin)]
public class AdminCountriesController : ControllerBase
{
    private readonly ICountryService _countryService;
    private readonly ILogger<AdminCountriesController> _logger;
    private readonly BusinessMetrics _businessMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminCountriesController"/> class.
    /// </summary>
    /// <param name="countryService">The country service instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="businessMetrics">The business metrics service.</param>
    public AdminCountriesController(
        ICountryService countryService,
        ILogger<AdminCountriesController> logger,
        BusinessMetrics businessMetrics)
    {
        _countryService = countryService;
        _logger = logger;
        _businessMetrics = businessMetrics;
    }

    /// <summary>
    /// Creates a new country with complete geographical and metadata information.
    /// Returns the created country with Location header pointing to the resource and ETag for future updates.
    /// </summary>
    /// <param name="request">Country creation request with all required fields (ISO2, ISO3, Name) and optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created country with generated ID and ETag.</returns>
    /// <response code="201">Country created successfully with Location and ETag headers.</response>
    /// <response code="400">Validation failed - missing or invalid required fields.</response>
    /// <response code="409">Conflict - ISO2 or ISO3 code already exists for another country.</response>
    [HttpPost]
    [RequirePermission(CountryPermissions.CountriesCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCountryRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            var result = await _countryService.CreateAsync(request, userId, cancellationToken);

            _logger.LogInformation("Country created: {Iso2} by user {UserId} with correlationId {CorrelationId}",
                result.Iso2, userId, HttpContext.TraceIdentifier);

            Response.Headers["Location"] = $"/country/v1/countries/{result.Id}";
            Response.Headers["ETag"] = result.ETag;

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Create", "POST", "201"); // Changed
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Country creation conflict: {Message} by user {UserId}", ex.Message, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Create", "POST", "409"); // Changed
            return Conflict(new { error = "ISO_CODE_CONFLICT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country creation failed for user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Create", "POST", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while creating the country" });
        }
    }

    /// <summary>
    /// Performs a full update (replacement) of an existing country.
    /// Requires If-Match header with current ETag to prevent lost updates (optimistic  concurrency control).
    /// All fields in the request will replace the existing values.
    /// </summary>
    /// <param name="id">The unique database ID of the country to update.</param>
    /// <param name="request">Complete country data for replacement.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated country with new ETag.</returns>
    /// <response code="200">Country updated successfully with new ETag header.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Country not found.</response>
    /// <response code="409">Conflict - ISO code already exists for another country.</response>
    /// <response code="412">Precondition failed - ETag mismatch, country was modified by another user.</response>
    /// <response code="428">Precondition required - If-Match header is missing.</response>
    [HttpPut("{id:guid}")]
    [RequirePermission(CountryPermissions.CountriesUpdate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCountryRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        // T102: Require If-Match header
        if (!Request.Headers.ContainsKey("If-Match"))
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Update", "PUT", "428"); // Changed
            return StatusCode(428, new { error = "PRECONDITION_REQUIRED", message = "If-Match header is required for updates" });
        }

        var ifMatch = Request.Headers["If-Match"].ToString();

        try
        {
            var result = await _countryService.UpdateAsync(id, request, ifMatch, userId, cancellationToken);

            _logger.LogInformation("Country updated: ID {Id}, {Iso2} by user {UserId} with correlationId {CorrelationId}",
                id, result.Iso2, userId, HttpContext.TraceIdentifier);

            Response.Headers["ETag"] = result.ETag;

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Update", "PUT", "200"); // Changed
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Country not found for update: ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Update", "PUT", "404"); // Changed
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Precondition failed") || ex.Message.Contains("Concurrency conflict"))
        {
            _logger.LogWarning("ETag mismatch on update: ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Update", "PUT", "412"); // Changed
            return StatusCode(412, new { error = "PRECONDITION_FAILED", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Country update conflict: {Message} by user {UserId}", ex.Message, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Update", "PUT", "409"); // Changed
            return Conflict(new { error = "ISO_CODE_CONFLICT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country update failed for ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Update", "PUT", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while updating the country" });
        }
    }

    /// <summary>
    /// Performs a partial update of an existing country, modifying only the specified fields.
    /// Requires If-Match header with current ETag for concurrency control.
    /// At least one field must be provided in the request.
    /// </summary>
    /// <param name="id">The unique database ID of the country to update.</param>
    /// <param name="request">Partial country data - only provided fields will be updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated country with new ETag.</returns>
    /// <response code="200">Country updated successfully with new ETag header.</response>
    /// <response code="400">Validation failed or no fields provided.</response>
    /// <response code="404">Country not found.</response>
    /// <response code="409">Conflict - ISO code already exists for another country.</response>
    /// <response code="412">Precondition failed - ETag mismatch.</response>
    /// <response code="428">Precondition required - If-Match header is missing.</response>
    [HttpPatch("{id:guid}")]
    [RequirePermission(CountryPermissions.CountriesUpdate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchCountryRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        // Require If-Match header
        if (!Request.Headers.ContainsKey("If-Match"))
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Patch", "PATCH", "428"); // Changed
            return StatusCode(428, new { error = "PRECONDITION_REQUIRED", message = "If-Match header is required for updates" });
        }

        var ifMatch = Request.Headers["If-Match"].ToString();

        try
        {
            var result = await _countryService.PatchAsync(id, request, ifMatch, userId, cancellationToken);

            _logger.LogInformation("Country patched: ID {Id}, {Iso2} by user {UserId} with correlationId {CorrelationId}",
                id, result.Iso2, userId, HttpContext.TraceIdentifier);

            Response.Headers["ETag"] = result.ETag;

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Patch", "PATCH", "200"); // Changed
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Patch", "PATCH", "404"); // Changed
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Precondition failed") || ex.Message.Contains("Concurrency conflict"))
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Patch", "PATCH", "412"); // Changed
            return StatusCode(412, new { error = "PRECONDITION_FAILED", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Patch", "PATCH", "409"); // Changed
            return Conflict(new { error = "ISO_CODE_CONFLICT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country patch failed for ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Patch", "PATCH", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while patching the country" });
        }
    }

    /// <summary>
    /// Soft deletes a country by setting its IsActive flag to false.
    /// The country data is preserved for audit and historical purposes but will not appear in normal queries.
    /// Can be reversed by updating the IsActive flag back to true.
    /// </summary>
    /// <param name="id">The unique database ID of the country to soft delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Country soft deleted successfully.</response>
    /// <response code="404">Country not found.</response>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CountryPermissions.CountriesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            await _countryService.SoftDeleteAsync(id, userId, cancellationToken);

            _logger.LogInformation("Country soft-deleted: ID {Id} by user {UserId} with correlationId {CorrelationId}",
                id, userId, HttpContext.TraceIdentifier);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "SoftDelete", "DELETE", "204"); // Changed
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "SoftDelete", "DELETE", "404"); // Changed
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country soft-delete failed for ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "SoftDelete", "DELETE", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while deleting the country" });
        }
    }

    /// <summary>
    /// Permanently deletes a country from the database.
    /// This operation cannot be undone and will remove all associated data.
    /// Requires SuperAdmin role. Use with extreme caution - prefer soft delete for most cases.
    /// </summary>
    /// <param name="id">The unique database ID of the country to permanently delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Country permanently deleted.</response>
    /// <response code="404">Country not found.</response>
    [HttpDelete("{id:guid}/hard-delete")]
    [RequirePermission(CountryPermissions.CountriesHardDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HardDelete(Guid id, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            await _countryService.HardDeleteAsync(id, userId, cancellationToken);

            _logger.LogWarning("Country HARD-DELETED: ID {Id} by user {UserId} with correlationId {CorrelationId}",
                id, userId, HttpContext.TraceIdentifier);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "HardDelete", "DELETE", "204"); // Changed
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "HardDelete", "DELETE", "404"); // Changed
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country hard-delete failed for ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "HardDelete", "DELETE", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while hard-deleting the country" });
        }
    }

    /// <summary>
    /// Restores a soft-deleted country.
    /// Sets IsActive to true and removes DeletedAt timestamp.
    /// </summary>
    /// <param name="id">The ID of the country to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Country restored successfully.</response>
    /// <response code="404">Country not found.</response>
    [HttpPost("{id:guid}/restore")]
    [RequirePermission(CountryPermissions.CountriesRestore)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            await _countryService.RestoreAsync(id, userId, cancellationToken);

            _logger.LogInformation("Country restored: ID {Id} by user {UserId} with correlationId {CorrelationId}",
                id, userId, HttpContext.TraceIdentifier);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Restore", "POST", "204");
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Restore", "POST", "404");
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country restore failed for ID {Id} by user {UserId}", id, userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "Restore", "POST", "500");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while restoring the country" });
        }
    }

    /// <summary>
    /// Rebuilds the country cache from database.
    /// Requires CountrySystemRebuildCache permission.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("rebuild-cache")]
    [RequirePermission(CountryPermissions.SystemRebuildCache)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RebuildCache(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            await _countryService.InvalidateListCachesAsync(cancellationToken);
            _logger.LogInformation("Cache rebuild triggered by user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "RebuildCache", "POST", "204");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache rebuild failed for user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "RebuildCache", "POST", "500");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while rebuilding the cache" });
        }
    }

    /// <summary>
    /// Exports all country data as JSON.
    /// Requires CountrySystemExport permission.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all countries.</returns>
    [HttpGet("export")]
    [RequirePermission(CountryPermissions.SystemExport)]
    [ProducesResponseType(typeof(IEnumerable<CountryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAll(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            var result = await _countryService.ListAsync(new CountryListRequest { PageSize = 1000 }, cancellationToken);
            _logger.LogInformation("Data export triggered by user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ExportAll", "GET", "200");
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data export failed for user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ExportAll", "GET", "500");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while exporting data" });
        }
    }



    /// <summary>
    /// Extracts the user identifier from JWT token claims for audit logging.
    /// Tries multiple claim types in order: 'sub', 'email', Identity.Name, or returns 'anonymous' if none found.
    /// </summary>
    /// <returns>The user identifier string.</returns>
    private string GetUserId()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name
            ?? "anonymous";

        _logger.LogDebug("Detected UserId: {UserId}", userId);
        return userId;
    }
}
