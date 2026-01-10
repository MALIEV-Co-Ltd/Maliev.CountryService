using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.CountryService.Data.Entities;

namespace Maliev.CountryService.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        // Primary Key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();

        // Foreign Key (NO constraint - allow audit after hard delete)
        builder.Property(a => a.CountryId).HasColumnName("country_id").IsRequired();
        builder.HasIndex(a => a.CountryId).HasDatabaseName("IX_audit_logs_country_id");

        // Operation and user tracking
        builder.Property(a => a.Operation).HasColumnName("operation").HasMaxLength(20).IsRequired();
        builder.Property(a => a.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_audit_logs_user_id");

        builder.Property(a => a.UserEmail).HasColumnName("user_email").HasMaxLength(255);
        builder.Property(a => a.UserRoles).HasColumnName("user_roles").HasColumnType("jsonb").HasDefaultValue("[]");

        // Snapshot data
        builder.Property(a => a.BeforeSnapshot).HasColumnName("before_snapshot").HasColumnType("jsonb");
        builder.Property(a => a.AfterSnapshot).HasColumnName("after_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.ChangedFields).HasColumnName("changed_fields").HasColumnType("jsonb").HasDefaultValue("[]");

        // Request metadata
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);

        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id");
        builder.HasIndex(a => a.CorrelationId).HasDatabaseName("IX_audit_logs_correlation_id");

        // Timestamp
        builder.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.HasIndex(a => a.CreatedAtUtc).HasDatabaseName("IX_audit_logs_created_at_utc");
    }
}
