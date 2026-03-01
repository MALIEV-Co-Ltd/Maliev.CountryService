using Maliev.CountryService.Application.Models.BulkImport;

namespace Maliev.CountryService.Application.Interfaces;

/// <summary>
/// Service interface for bulk country import operations.
/// </summary>
public interface IBulkImportService
{
    /// <summary>
    /// Validates a bulk import request (Phase 1: validation only, no database writes).
    /// </summary>
    /// <param name="request">The bulk import request containing countries to validate.</param>
    /// <param name="userId">The ID of the user initiating the import.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A status response containing validation results and errors.</returns>
    Task<BulkImportStatusResponse> ValidateImportAsync(BulkImportRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a validated bulk import job (Phase 2: atomic database writes).
    /// </summary>
    /// <param name="jobId">The unique identifier of the validated job to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A status response containing processing results.</returns>
    Task<BulkImportStatusResponse> ProcessImportAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a bulk import job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A status response containing the current job status, or null if not found.</returns>
    Task<BulkImportStatusResponse?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
}
