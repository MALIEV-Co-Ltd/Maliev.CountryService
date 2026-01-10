using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Maliev.Aspire.ServiceDefaults.Database;
using System.Text.RegularExpressions; // Added for Regex

namespace Maliev.CountryService.Data;

public class CountryDbContext : DbContext
{
    public CountryDbContext(DbContextOptions<CountryDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; }
    public DbSet<BulkImportJob> BulkImportJobs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // T033: Enable pg_trgm extension for high-performance full-text search
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Iso2).IsUnique();
            entity.HasIndex(e => e.Iso3).IsUnique().HasFilter("iso3 IS NOT NULL"); // Only unique for non-null values

            // T033: GIN index for high-performance full-text search (requires pg_trgm extension)
            entity.HasIndex(e => e.Name)
                  .HasMethod("gin")
                  .HasOperators("gin_trgm_ops");

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OfficialName).HasMaxLength(200);
            entity.Property(e => e.NumericCode).HasMaxLength(3);
            entity.Property(e => e.Capital).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.Subregion).HasMaxLength(50);
            entity.Property(e => e.Demonym).HasMaxLength(50);
            entity.Property(e => e.Iso2).IsRequired().HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.Iso3).HasMaxLength(3).IsFixedLength();

            // Explicitly map double properties to avoid decimal inference
            entity.Property(e => e.Latitude).HasColumnType("double precision");
            entity.Property(e => e.Longitude).HasColumnType("double precision");
            entity.Property(e => e.AreaKm2).HasColumnType("double precision");
            entity.Property(e => e.GiniCoefficient).HasColumnType("double precision");

            // JSONB properties
            entity.Property(e => e.Timezones).HasColumnType("jsonb");
            entity.Property(e => e.Borders).HasColumnType("jsonb");
            entity.Property(e => e.CallingCodes).HasColumnType("jsonb");
            entity.Property(e => e.TopLevelDomains).HasColumnType("jsonb");
            entity.Property(e => e.Currencies).HasColumnType("jsonb");
            entity.Property(e => e.Languages).HasColumnType("jsonb");
            entity.Property(e => e.Translations).HasColumnType("jsonb");
            entity.Property(e => e.Flags).HasColumnType("jsonb");
            entity.Property(e => e.CoatOfArms).HasColumnType("jsonb");

            // Audit fields
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.LastModifiedUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp with time zone");

            entity.Property(e => e.IsActive).IsRequired();

            entity.Property(e => e.Version).IsConcurrencyToken(); // Optimistic concurrency

            // Global query filter to only include active countries by default
            entity.HasQueryFilter(e => e.IsActive);
        });

        modelBuilder.Entity<BulkImportJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TotalRecords).IsRequired();
            entity.Property(e => e.ProcessedRecords).IsRequired();
            entity.Property(e => e.FailedRecords).IsRequired();
            entity.Property(e => e.ValidationErrors).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.StartedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.PayloadData).HasColumnType("jsonb"); // Configure PayloadData as jsonb
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(e => e.Country)
                  .WithMany()
                  .HasForeignKey(e => e.CountryId)
                  .IsRequired(false) // Fix for global query filter warning
                  .OnDelete(DeleteBehavior.SetNull); // Preserve audit logs after hard delete
        });

        // Apply PostgreSQL snake_case naming convention globally once
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
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
