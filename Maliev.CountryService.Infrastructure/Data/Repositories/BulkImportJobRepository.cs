using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CountryService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementation of IBulkImportJobRepository using Entity Framework Core.
/// </summary>
public class BulkImportJobRepository : IBulkImportJobRepository
{
    private readonly CountryDbContext _context;

    /// <summary>
    /// Initializes a new instance of the BulkImportJobRepository class.
    /// </summary>
    public BulkImportJobRepository(CountryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<BulkImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BulkImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BulkImportJob>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.BulkImportJobs
            .AsNoTracking()
            .Where(j => j.Status == "Validated")
            .OrderBy(j => j.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BulkImportJob> AddAsync(BulkImportJob job, CancellationToken cancellationToken = default)
    {
        _context.BulkImportJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(BulkImportJob job, CancellationToken cancellationToken = default)
    {
        _context.BulkImportJobs.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> ClaimNextValidatedJobAsync(Guid workerId, CancellationToken cancellationToken = default)
    {
        return await _context.BulkImportJobs
            .Where(j => j.Status == "Validated")
            .OrderBy(j => j.CreatedAtUtc)
            .Take(1)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, "Processing")
                .SetProperty(j => j.StartedAtUtc, DateTime.UtcNow)
                .SetProperty(j => j.ClaimedByWorkerId, workerId),
                cancellationToken);
    }
}
