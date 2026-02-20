using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.CountryService.Data.Configurations;

public class BulkImportJobConfiguration : IEntityTypeConfiguration<BulkImportJob>
{
    public void Configure(EntityTypeBuilder<BulkImportJob> builder)
    {
        builder.ToTable("bulk_import_jobs");

        // Primary Key
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Status tracking
        builder.Property(b => b.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending");
        builder.HasIndex(b => b.Status).HasDatabaseName("IX_bulk_import_jobs_status");

        // Worker claiming for atomic job processing
        builder.Property(b => b.ClaimedByWorkerId).HasColumnName("claimed_by_worker_id");
        builder.HasIndex(b => b.ClaimedByWorkerId).HasDatabaseName("ix_bulk_import_jobs_claimed_by_worker_id");

        // Record counts
        builder.Property(b => b.TotalRecords).HasColumnName("total_records").IsRequired();
        builder.Property(b => b.ProcessedRecords).HasColumnName("processed_records").HasDefaultValue(0);
        builder.Property(b => b.FailedRecords).HasColumnName("failed_records").HasDefaultValue(0);

        // Validation and error data
        builder.Property(b => b.ValidationErrors).HasColumnName("validation_errors").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(b => b.ErrorMessage).HasColumnName("error_message").HasColumnType("text");

        // User tracking
        builder.Property(b => b.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
        builder.HasIndex(b => b.UserId).HasDatabaseName("IX_bulk_import_jobs_user_id");

        builder.Property(b => b.UserEmail).HasColumnName("user_email").HasMaxLength(255);
        builder.Property(b => b.IpAddress).HasColumnName("ip_address").HasMaxLength(45);

        // Request tracking
        builder.Property(b => b.CorrelationId).HasColumnName("correlation_id");
        builder.HasIndex(b => b.CorrelationId).HasDatabaseName("IX_bulk_import_jobs_correlation_id");

        // Timestamps
        builder.Property(b => b.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.HasIndex(b => b.CreatedAtUtc).HasDatabaseName("IX_bulk_import_jobs_created_at_utc").IsDescending();

        builder.Property(b => b.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(b => b.CompletedAtUtc).HasColumnName("completed_at_utc");

        // Computed property - not mapped to database
        builder.Ignore(b => b.DurationMs);
    }
}
