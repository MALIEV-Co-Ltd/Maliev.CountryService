using Asp.Versioning;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Api.Models.BulkImport;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.CountryService.Api.Controllers;

/// <summary>
/// T115: Bulk import controller for administrative country data imports.
/// Supports two-phase validation: validate first, then process.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("countries/v{version:apiVersion}/admin/bulk-import")]
[EnableRateLimiting("admin-endpoints")]
[Authorize(Policy = "CountryAdmin")]
public class BulkImportController : ControllerBase
{
    private readonly IBulkImportService _bulkImportService;
    private readonly ILogger<BulkImportController> _logger;

    public BulkImportController(IBulkImportService bulkImportService, ILogger<BulkImportController> logger)
    {
        _bulkImportService = bulkImportService;
        _logger = logger;
    }

    /// <summary>
    /// T116: POST /admin/bulk-import - Submit bulk import request for validation.
    /// Returns 202 with Location header if validation passes.
    /// Returns 400 with validation errors if validation fails.
    /// T119: Returns 413 if payload exceeds 1000 countries.
    /// </summary>
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

            BusinessMetrics.RequestDuration.WithLabels("BulkImportSubmit", "POST", "413").Observe(stopwatch.Elapsed.TotalSeconds);
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
            BusinessMetrics.RequestDuration.WithLabels("BulkImportSubmit", "POST", "400").Observe(stopwatch.Elapsed.TotalSeconds);
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

                BusinessMetrics.RequestDuration.WithLabels("BulkImportSubmit", "POST", "400").Observe(stopwatch.Elapsed.TotalSeconds);
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

            BusinessMetrics.RequestDuration.WithLabels("BulkImportSubmit", "POST", "202").Observe(stopwatch.Elapsed.TotalSeconds);
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId = result.JobId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import submission failed for user {UserId}", userId);
            BusinessMetrics.RequestDuration.WithLabels("BulkImportSubmit", "POST", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while processing the bulk import" });
        }
    }

    /// <summary>
    /// T117: GET /admin/bulk-import/{jobId} - Get bulk import job status.
    /// Returns job status, validation errors, and progress.
    /// </summary>
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
                BusinessMetrics.RequestDuration.WithLabels("GetJobStatus", "GET", "404").Observe(stopwatch.Elapsed.TotalSeconds);
                return NotFound(new { error = "JOB_NOT_FOUND", message = $"Bulk import job {jobId} not found" });
            }

            BusinessMetrics.RequestDuration.WithLabels("GetJobStatus", "GET", "200").Observe(stopwatch.Elapsed.TotalSeconds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
            BusinessMetrics.RequestDuration.WithLabels("GetJobStatus", "GET", "500").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while retrieving job status" });
        }
    }

    /// <summary>
    /// T118: POST /admin/bulk-import/{jobId}/process - Trigger processing of validated job.
    /// Returns 202 if processing started.
    /// Returns 400 if job is not in Validated status.
    /// </summary>
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
                BusinessMetrics.RequestDuration.WithLabels("ProcessJob", "POST", "404").Observe(stopwatch.Elapsed.TotalSeconds);
                return NotFound(new { error = "JOB_NOT_FOUND", message = $"Bulk import job {jobId} not found" });
            }

            if (jobStatus.Status != "Validated")
            {
                _logger.LogWarning("Attempt to process job {JobId} in status {Status} by user {UserId}",
                    jobId, jobStatus.Status, userId);

                BusinessMetrics.RequestDuration.WithLabels("ProcessJob", "POST", "400").Observe(stopwatch.Elapsed.TotalSeconds);
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

            BusinessMetrics.RequestDuration.WithLabels("ProcessJob", "POST", "202").Observe(stopwatch.Elapsed.TotalSeconds);
            return AcceptedAtAction(nameof(GetJobStatus), new { jobId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            BusinessMetrics.RequestDuration.WithLabels("ProcessJob", "POST", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "JOB_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            BusinessMetrics.RequestDuration.WithLabels("ProcessJob", "POST", "400").Observe(stopwatch.Elapsed.TotalSeconds);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            BusinessMetrics.RequestDuration.WithLabels("ProcessJob", "POST", "500").Observe(stopwatch.Elapsed.TotalSeconds);
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
