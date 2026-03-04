using Maliev.CountryService.Domain.Entities;

namespace Maliev.CountryService.Application.Interfaces;

/// <summary>
/// Repository interface for BulkImportJob entity operations.
/// </summary>
public interface IBulkImportJobRepository
{
    /// <summary>
    /// Gets a bulk import job by its unique identifier.
    /// </summary>
    Task<BulkImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending validated jobs for processing.
    /// </summary>
    Task<IReadOnlyList<BulkImportJob>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new bulk import job.
    /// </summary>
    Task<BulkImportJob> AddAsync(BulkImportJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing bulk import job.
    /// </summary>
    Task UpdateAsync(BulkImportJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims the next validated job for processing by a worker.
    /// </summary>
    Task<int> ClaimNextValidatedJobAsync(Guid workerId, CancellationToken cancellationToken = default);
}
