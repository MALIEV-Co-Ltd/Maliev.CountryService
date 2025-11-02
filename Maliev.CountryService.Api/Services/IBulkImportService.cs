using Maliev.CountryService.Api.Models.BulkImport;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// T107: Service interface for bulk country import operations.
/// </summary>
public interface IBulkImportService
{
    /// <summary>
    /// T109: Validate bulk import request (Phase 1: validation only, no database writes).
    /// </summary>
    Task<BulkImportStatusResponse> ValidateImportAsync(BulkImportRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// T110: Process validated bulk import job (Phase 2: atomic database writes).
    /// </summary>
    Task<BulkImportStatusResponse> ProcessImportAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get bulk import job status.
    /// </summary>
    Task<BulkImportStatusResponse?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
}
