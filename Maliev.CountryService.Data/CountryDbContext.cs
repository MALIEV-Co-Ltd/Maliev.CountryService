using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions; // Added for Regex

namespace Maliev.CountryService.Data;

public class CountryDbContext : DbContext
{
    public CountryDbContext(DbContextOptions<CountryDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<BulkImportJob> BulkImportJobs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pg_trgm extension for high-performance full-text search
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CountryDbContext).Assembly);

        // Apply PostgreSQL snake_case naming convention globally
        Maliev.Aspire.ServiceDefaults.Database.SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }
}

public static class StringExtensions
{
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
