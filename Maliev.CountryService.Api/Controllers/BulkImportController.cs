using Asp.Versioning;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Api.Models.BulkImport;
using Maliev.CountryService.Api.Services;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.IO;

namespace Maliev.CountryService.Api.Controllers;

/// <summary>
/// Administrative endpoints for bulk importing country data.
/// Implements a two-phase approach: validation phase (returns validation errors immediately) followed by processing phase (executed asynchronously).
/// Maximum 1000 countries per batch. All operations logged with full audit trail.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("country/v{version:apiVersion}/admin/bulk-import")]
[EnableRateLimiting("admin-endpoints")]
public class BulkImportController : ControllerBase
{
    private readonly IBulkImportService _bulkImportService;
    private readonly ILogger<BulkImportController> _logger;
    private readonly BusinessMetrics _businessMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkImportController"/> class.
    /// </summary>
    public BulkImportController(
        IBulkImportService bulkImportService,
        ILogger<BulkImportController> logger,
        BusinessMetrics businessMetrics)
    {
        _bulkImportService = bulkImportService;
        _logger = logger;
        _businessMetrics = businessMetrics;
    }

    /// <summary>
    /// Submits a bulk import request for validation.
    /// </summary>
    [HttpPost]
    [RequirePermission(CountryPermissions.ImportUpload)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> SubmitBulkImport([FromBody] BulkImportRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        if (request.Countries.Count > 1000)
        {
            _logger.LogWarning("Bulk import rejected: {Count} countries exceeds limit of 1000 by user {UserId}",
                request.Countries.Count, userId);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "413");
            return StatusCode(413, new
            {
                error = "PAYLOAD_TOO_LARGE",
                message = "Bulk import requests are limited to 1000 countries per batch",
                limit = 1000,
                received = request.Countries.Count
            });
        }

        if (request.Countries.Count == 0)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "400");
            return BadRequest(new { error = "EMPTY_REQUEST", message = "At least one country is required" });
        }

        try
        {
            var result = await _bulkImportService.ValidateImportAsync(request, userId, cancellationToken);

            _logger.LogInformation("Bulk import submitted: JobId {JobId}, Status {Status}, Records {Count} by user {UserId}",
                result.JobId, result.Status, result.TotalRecords, userId);

            if (result.Status == "ValidationFailed")
            {
                _logger.LogWarning("Bulk import validation failed: JobId {JobId}, Errors {ErrorCount}",
                    result.JobId, result.ValidationErrors.Count);

                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "400");
                return BadRequest(new
                {
                    error = "VALIDATION_FAILED",
                    message = "Bulk import validation failed",
                    jobId = result.JobId,
                    validationErrors = result.ValidationErrors
                });
            }

            Response.Headers["Location"] = $"/countries/v1/admin/bulk-import/{result.JobId}";

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "202");
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId = result.JobId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import submission failed for user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "500");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while processing the bulk import" });
        }
    }

    /// <summary>
    /// Retrieves the current status and progress of a bulk import job.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [RequirePermission(CountryPermissions.ImportStatus)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await _bulkImportService.GetJobStatusAsync(jobId, cancellationToken);

            if (result == null)
            {
                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "GetJobStatus", "GET", "404");
                return NotFound(new { error = "JOB_NOT_FOUND", message = $"Bulk import job {jobId} not found" });
            }

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "GetJobStatus", "GET", "200");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "GetJobStatus", "GET", "500");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while retrieving job status" });
        }
    }

    /// <summary>
    /// Triggers asynchronous processing of a validated bulk import job.
    /// </summary>
    [HttpPost("{jobId:guid}/process")]
    [RequirePermission(CountryPermissions.ImportExecute)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessJob(Guid jobId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            var jobStatus = await _bulkImportService.GetJobStatusAsync(jobId, cancellationToken);

            if (jobStatus == null)
            {
                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "404");
                return NotFound(new { error = "JOB_NOT_FOUND", message = $"Bulk import job {jobId} not found" });
            }

            if (jobStatus.Status != "Validated")
            {
                _logger.LogWarning("Attempt to process job {JobId} in status {Status} by user {UserId}",
                    jobId, jobStatus.Status, userId);

                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "400");
                return BadRequest(new
                {
                    error = "INVALID_JOB_STATUS",
                    message = $"Job is in status '{jobStatus.Status}'. Only 'Validated' jobs can be processed.",
                    currentStatus = jobStatus.Status
                });
            }

            var result = await _bulkImportService.ProcessImportAsync(jobId, cancellationToken);

            _logger.LogInformation("Bulk import processing triggered: JobId {JobId} by user {UserId}", jobId, userId);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "202");
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "500");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while processing the job" });
        }
    }

    private string GetUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name
            ?? "anonymous";
    }
}
