using Asp.Versioning;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.CountryService.Api.Controllers;

/// <summary>
/// T093-T098: Admin controller for country CRUD operations.
/// Requires authentication and role-based authorization (CountryAdmin or SuperAdmin).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("countries/v{version:apiVersion}/admin/countries")]
[EnableRateLimiting("admin-endpoints")]
[Authorize(Policy = "CountryAdmin")]
public class AdminCountriesController : ControllerBase
{
    private readonly ICountryService _countryService;
    private readonly ILogger<AdminCountriesController> _logger;

    public AdminCountriesController(ICountryService countryService, ILogger<AdminCountriesController> logger)
    {
        _countryService = countryService;
        _logger = logger;
    }

    /// <summary>
    /// T093: POST /admin/countries - Create a new country.
    /// Returns 201 with Location header and ETag.
    /// Returns 400 if validation fails, 409 if ISO code conflict.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCountryRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            var result = await _countryService.CreateAsync(request, userId, cancellationToken);

            _logger.LogInformation("Country created: {Iso2} by user {UserId} with correlationId {CorrelationId}",
                result.Iso2, userId, HttpContext.TraceIdentifier);

            Response.Headers["Location"] = $"/countries/v1/countries/{result.Id}";
            Response.Headers["ETag"] = result.ETag;

            BusinessMetrics.RequestDuration.WithLabels("Create", "POST", "201").Observe(stopwatch.Elapsed.TotalSeconds);
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Country creation conflict: {Message} by user {UserId}", ex.Message, userId);
            BusinessMetrics.RequestDuration.WithLabels("Create", "POST", "409").Observe(stopwatch.Elapsed.TotalSeconds);
            return Conflict(new { error = "ISO_CODE_CONFLICT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country creation failed for user {UserId}", userId);
            BusinessMetrics.RequestDuration.WithLabels("Create", "POST", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while creating the country" });
        }
    }

    /// <summary>
    /// T094: PUT /admin/countries/{id} - Full update of a country.
    /// Requires If-Match header with ETag.
    /// Returns 200 with new ETag on success.
    /// Returns 412 if version mismatch, 409 if ISO code conflict.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCountryRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        // T102: Require If-Match header
        if (!Request.Headers.ContainsKey("If-Match"))
        {
            BusinessMetrics.RequestDuration.WithLabels("Update", "PUT", "428").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(428, new { error = "PRECONDITION_REQUIRED", message = "If-Match header is required for updates" });
        }

        var ifMatch = Request.Headers["If-Match"].ToString();

        try
        {
            var result = await _countryService.UpdateAsync(id, request, ifMatch, userId, cancellationToken);

            _logger.LogInformation("Country updated: ID {Id}, {Iso2} by user {UserId} with correlationId {CorrelationId}",
                id, result.Iso2, userId, HttpContext.TraceIdentifier);

            Response.Headers["ETag"] = result.ETag;

            BusinessMetrics.RequestDuration.WithLabels("Update", "PUT", "200").Observe(stopwatch.Elapsed.TotalSeconds);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Country not found for update: ID {Id} by user {UserId}", id, userId);
            BusinessMetrics.RequestDuration.WithLabels("Update", "PUT", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Precondition failed"))
        {
            _logger.LogWarning("ETag mismatch on update: ID {Id} by user {UserId}", id, userId);
            BusinessMetrics.RequestDuration.WithLabels("Update", "PUT", "412").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(412, new { error = "PRECONDITION_FAILED", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Country update conflict: {Message} by user {UserId}", ex.Message, userId);
            BusinessMetrics.RequestDuration.WithLabels("Update", "PUT", "409").Observe(stopwatch.Elapsed.TotalSeconds);
            return Conflict(new { error = "ISO_CODE_CONFLICT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country update failed for ID {Id} by user {UserId}", id, userId);
            BusinessMetrics.RequestDuration.WithLabels("Update", "PUT", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while updating the country" });
        }
    }

    /// <summary>
    /// T095: PATCH /admin/countries/{id} - Partial update of a country.
    /// Requires If-Match header. At least one field must be provided.
    /// </summary>
    [HttpPatch("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Patch(long id, [FromBody] PatchCountryRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        // Require If-Match header
        if (!Request.Headers.ContainsKey("If-Match"))
        {
            BusinessMetrics.RequestDuration.WithLabels("Patch", "PATCH", "428").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(428, new { error = "PRECONDITION_REQUIRED", message = "If-Match header is required for updates" });
        }

        var ifMatch = Request.Headers["If-Match"].ToString();

        try
        {
            var result = await _countryService.PatchAsync(id, request, ifMatch, userId, cancellationToken);

            _logger.LogInformation("Country patched: ID {Id}, {Iso2} by user {UserId} with correlationId {CorrelationId}",
                id, result.Iso2, userId, HttpContext.TraceIdentifier);

            Response.Headers["ETag"] = result.ETag;

            BusinessMetrics.RequestDuration.WithLabels("Patch", "PATCH", "200").Observe(stopwatch.Elapsed.TotalSeconds);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            BusinessMetrics.RequestDuration.WithLabels("Patch", "PATCH", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Precondition failed"))
        {
            BusinessMetrics.RequestDuration.WithLabels("Patch", "PATCH", "412").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(412, new { error = "PRECONDITION_FAILED", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            BusinessMetrics.RequestDuration.WithLabels("Patch", "PATCH", "409").Observe(stopwatch.Elapsed.TotalSeconds);
            return Conflict(new { error = "ISO_CODE_CONFLICT", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country patch failed for ID {Id} by user {UserId}", id, userId);
            BusinessMetrics.RequestDuration.WithLabels("Patch", "PATCH", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while patching the country" });
        }
    }

    /// <summary>
    /// T096: DELETE /admin/countries/{id} - Soft delete a country.
    /// Sets IsActive=false. Requires CountryAdmin role.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(long id, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            await _countryService.SoftDeleteAsync(id, userId, cancellationToken);

            _logger.LogInformation("Country soft-deleted: ID {Id} by user {UserId} with correlationId {CorrelationId}",
                id, userId, HttpContext.TraceIdentifier);

            BusinessMetrics.RequestDuration.WithLabels("SoftDelete", "DELETE", "204").Observe(stopwatch.Elapsed.TotalSeconds);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            BusinessMetrics.RequestDuration.WithLabels("SoftDelete", "DELETE", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country soft-delete failed for ID {Id} by user {UserId}", id, userId);
            BusinessMetrics.RequestDuration.WithLabels("SoftDelete", "DELETE", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while deleting the country" });
        }
    }

    /// <summary>
    /// T097: DELETE /admin/countries/{id}/hard-delete - Permanently delete a country.
    /// Requires SuperAdmin role. Use with extreme caution!
    /// </summary>
    [HttpDelete("{id:long}/hard-delete")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HardDelete(long id, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            await _countryService.HardDeleteAsync(id, userId, cancellationToken);

            _logger.LogWarning("Country HARD-DELETED: ID {Id} by user {UserId} with correlationId {CorrelationId}",
                id, userId, HttpContext.TraceIdentifier);

            BusinessMetrics.RequestDuration.WithLabels("HardDelete", "DELETE", "204").Observe(stopwatch.Elapsed.TotalSeconds);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            BusinessMetrics.RequestDuration.WithLabels("HardDelete", "DELETE", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Country hard-delete failed for ID {Id} by user {UserId}", id, userId);
            BusinessMetrics.RequestDuration.WithLabels("HardDelete", "DELETE", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while hard-deleting the country" });
        }
    }

    /// <summary>
    /// T090: Extract user context from JWT claims.
    /// </summary>
    private string GetUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name
            ?? "anonymous";
    }
}
