using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions; // Added for Regex

namespace Maliev.CountryService.Data;

public class CountryServiceDbContext : DbContext
{
    public CountryServiceDbContext(DbContextOptions<CountryServiceDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; }
    public DbSet<BulkImportJob> BulkImportJobs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Replace table names
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Replace column names
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName()?.ToSnakeCase());
            }

            // Replace key names
            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            // Replace foreign key names
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(foreignKey.GetConstraintName()?.ToSnakeCase());
            }

            // Replace index names
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Iso2).IsUnique();
            entity.HasIndex(e => e.Iso3).IsUnique().HasFilter("iso3 IS NOT NULL"); // Only unique for non-null values
            entity.HasIndex(e => e.Name); // Regular B-tree index for Name sorting and filtering
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
