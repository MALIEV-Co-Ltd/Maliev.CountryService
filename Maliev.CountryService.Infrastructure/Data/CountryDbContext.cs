using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Maliev.CountryService.Infrastructure.Data;

/// <summary>
/// EF Core database context for the Country service.
/// Implements <see cref="ICountryDbContext"/> to allow application-layer abstraction.
/// </summary>
public class CountryDbContext : DbContext, ICountryDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountryDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public CountryDbContext(DbContextOptions<CountryDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<Country> Countries { get; set; } = null!;

    /// <inheritdoc />
    public DbSet<BulkImportJob> BulkImportJobs { get; set; } = null!;

    /// <inheritdoc />
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pg_trgm extension for high-performance full-text search
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CountryDbContext).Assembly);

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }
}

/// <summary>
/// Provides string extension methods for snake_case conversion.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a PascalCase or camelCase string to snake_case.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The converted snake_case string, or null/empty if input is null/empty.</returns>
    public static string? ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var startUnderscores = Regex.Match(input, @"^_+");
        return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
}
