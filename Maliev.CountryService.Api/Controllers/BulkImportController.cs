using Asp.Versioning;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Api.Models.BulkImport;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Added
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json; // Added
using System.IO; // Added

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
[Authorize(Policy = "CountryAdmin")]
public class BulkImportController : ControllerBase
{
    private readonly IBulkImportService _bulkImportService;
    private readonly ILogger<BulkImportController> _logger;
    private readonly BusinessMetrics _businessMetrics; // Added
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkImportController"/> class.
    /// </summary>
    /// <param name="bulkImportService">The bulk import service instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="businessMetrics">The business metrics service.</param> // Added
    public BulkImportController(
        IBulkImportService bulkImportService, 
        ILogger<BulkImportController> logger,
        BusinessMetrics businessMetrics) // Added
    {
        _bulkImportService = bulkImportService;
        _logger = logger;
        _businessMetrics = businessMetrics; // Added
    }

    /// <summary>
    /// Submits a bulk import request for validation.
    /// The system validates all country data (ISO codes, required fields, format checks) and returns validation errors immediately.
    /// If validation passes, you must call the process endpoint separately to apply the changes.
    /// Maximum 1000 countries per batch.
    /// </summary>
    /// <param name="request">Bulk import request containing array of countries to import/update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job status with validation results. Location header points to job status endpoint.</returns>
    /// <response code="202">Validation successful - job created. Use the Location header to check status or call the process endpoint.</response>
    /// <response code="400">Validation failed - returns detailed validation errors for each country.</response>
    /// <response code="413">Payload too large - exceeds 1000 country limit.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> SubmitBulkImport([FromBody] BulkImportRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        // T119: Validate payload size
        if (request.Countries.Count > 1000)
        {
            _logger.LogWarning("Bulk import rejected: {Count} countries exceeds limit of 1000 by user {UserId}",
                request.Countries.Count, userId);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "413"); // Changed
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
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "400"); // Changed
            return BadRequest(new { error = "EMPTY_REQUEST", message = "At least one country is required" });
        }

        try
        {
            // T116: Validate import
            var result = await _bulkImportService.ValidateImportAsync(request, userId, cancellationToken);

            _logger.LogInformation("Bulk import submitted: JobId {JobId}, Status {Status}, Records {Count} by user {UserId}",
                result.JobId, result.Status, result.TotalRecords, userId);

            if (result.Status == "ValidationFailed")
            {
                // T116: Return 400 with validation errors
                _logger.LogWarning("Bulk import validation failed: JobId {JobId}, Errors {ErrorCount}",
                    result.JobId, result.ValidationErrors.Count);

                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "400"); // Changed
                return BadRequest(new
                {
                    error = "VALIDATION_FAILED",
                    message = "Bulk import validation failed",
                    jobId = result.JobId,
                    validationErrors = result.ValidationErrors
                });
            }

            // T116: Return 202 with Location header
            Response.Headers["Location"] = $"/countries/v1/admin/bulk-import/{result.JobId}";

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "202"); // Changed
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId = result.JobId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import submission failed for user {UserId}", userId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "BulkImportSubmit", "POST", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while processing the bulk import" });
        }
    }

    /// <summary>
    /// Retrieves the current status and progress of a bulk import job.
    /// Returns validation errors, processing progress, and final results.
    /// </summary>
    /// <param name="jobId">The unique identifier of the bulk import job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job status including validation errors, progress, and processing results.</returns>
    /// <response code="200">Returns job status with detailed information.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("{jobId:guid}")]
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
                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "GetJobStatus", "GET", "404"); // Changed
                return NotFound(new { error = "JOB_NOT_FOUND", message = $"Bulk import job {jobId} not found" });
            }

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "GetJobStatus", "GET", "200"); // Changed
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "GetJobStatus", "GET", "500"); // Changed
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while retrieving job status" });
        }
    }

    /// <summary>
    /// Triggers asynchronous processing of a validated bulk import job.
    /// Only jobs in 'Validated' status can be processed. Processing happens asynchronously in the background.
    /// Use the job status endpoint to monitor progress.
    /// </summary>
    /// <param name="jobId">The unique identifier of the validated bulk import job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Accepted status with link to job status endpoint.</returns>
    /// <response code="202">Processing started - job is being processed asynchronously.</response>
    /// <response code="400">Invalid job status - job must be in 'Validated' status.</response>
    /// <response code="404">Job not found.</response>
    [HttpPost("{jobId:guid}/process")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessJob(Guid jobId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var userId = GetUserId();

        try
        {
            // Check job exists and is in correct status
            var jobStatus = await _bulkImportService.GetJobStatusAsync(jobId, cancellationToken);

            if (jobStatus == null)
            {
                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "404"); // Changed
                return NotFound(new { error = "JOB_NOT_FOUND", message = $"Bulk import job {jobId} not found" });
            }

            if (jobStatus.Status != "Validated")
            {
                _logger.LogWarning("Attempt to process job {JobId} in status {Status} by user {UserId}",
                    jobId, jobStatus.Status, userId);

                _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "400"); // Changed
                return BadRequest(new
                {
                    error = "INVALID_JOB_STATUS",
                    message = $"Job is in status '{jobStatus.Status}'. Only 'Validated' jobs can be processed.",
                    currentStatus = jobStatus.Status
                });
            }

            // Note: Actual processing will be done by the background worker
            // This endpoint just initiates the process
            var result = await _bulkImportService.ProcessImportAsync(jobId, cancellationToken);

            _logger.LogInformation("Bulk import processing triggered: JobId {JobId} by user {UserId}", jobId, userId);

            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "202"); // Changed
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "404"); // Changed
            return NotFound(new { error = "JOB_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "400"); // Changed
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            _businessMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "ProcessJob", "POST", "500"); // Changed
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
