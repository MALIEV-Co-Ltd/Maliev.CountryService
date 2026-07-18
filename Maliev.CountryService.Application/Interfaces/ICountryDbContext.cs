using Maliev.CountryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Maliev.CountryService.Application.Interfaces;

/// <summary>
/// Abstraction over the country database context for use-case services.
/// Allows infrastructure to be swapped without touching application logic.
/// </summary>
public interface ICountryDbContext
{
    /// <summary>
    /// Gets the countries DbSet.
    /// </summary>
    DbSet<Country> Countries { get; }

    /// <summary>
    /// Gets the audit logs DbSet.
    /// </summary>
    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Gets the bulk import jobs DbSet.
    /// </summary>
    DbSet<BulkImportJob> BulkImportJobs { get; }

    /// <summary>
    /// Gets the database facade for executing raw SQL and managing transactions.
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// Saves all pending changes to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
