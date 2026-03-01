using Maliev.CountryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.CountryService.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity configuration for the <see cref="Country"/> entity.
/// </summary>
public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    /// <summary>
    /// Configures the <see cref="Country"/> entity schema.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");

        // Primary Key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // ISO Codes with unique indexes
        builder.Property(c => c.Iso2).HasColumnName("iso2").HasMaxLength(2).IsRequired();
        builder.HasIndex(c => c.Iso2).IsUnique().HasDatabaseName("UQ_countries_iso2");

        builder.Property(c => c.Iso3).HasColumnName("iso3").HasMaxLength(3).IsRequired();
        builder.HasIndex(c => c.Iso3).IsUnique().HasDatabaseName("UQ_countries_iso3");

        // Name with GIN index for full-text search
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).HasDatabaseName("IX_countries_name_gin");

        // Basic attributes
        builder.Property(c => c.OfficialName).HasColumnName("official_name").HasMaxLength(200);
        builder.Property(c => c.NumericCode).HasColumnName("numeric_code").HasMaxLength(3);
        builder.Property(c => c.Capital).HasColumnName("capital").HasMaxLength(100);

        builder.Property(c => c.Region).HasColumnName("region").HasMaxLength(50);
        builder.HasIndex(c => c.Region).HasDatabaseName("IX_countries_region");

        builder.Property(c => c.Subregion).HasColumnName("subregion").HasMaxLength(50);

        // Coordinates
        builder.Property(c => c.Latitude).HasColumnName("latitude").HasPrecision(10, 8);
        builder.Property(c => c.Longitude).HasColumnName("longitude").HasPrecision(11, 8);

        // Demographics and geography
        builder.Property(c => c.Demonym).HasColumnName("demonym").HasMaxLength(50);
        builder.Property(c => c.AreaKm2).HasColumnName("area_km2").HasPrecision(15, 2);
        builder.Property(c => c.Population).HasColumnName("population");
        builder.Property(c => c.GiniCoefficient).HasColumnName("gini_coefficient").HasPrecision(4, 2);

        // JSONB columns
        builder.Property(c => c.Timezones).HasColumnName("timezones").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(c => c.Borders).HasColumnName("borders").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(c => c.CallingCodes).HasColumnName("calling_codes").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(c => c.TopLevelDomains).HasColumnName("top_level_domains").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(c => c.Currencies).HasColumnName("currencies").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.Languages).HasColumnName("languages").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.Translations).HasColumnName("translations").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.Flags).HasColumnName("flags").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.CoatOfArms).HasColumnName("coat_of_arms").HasColumnType("jsonb");

        // Boolean flags
        builder.Property(c => c.Independent).HasColumnName("independent").HasDefaultValue(false);
        builder.Property(c => c.UnMember).HasColumnName("un_member").HasDefaultValue(false);
        builder.Property(c => c.Landlocked).HasColumnName("landlocked").HasDefaultValue(false);

        // Soft delete
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasIndex(c => c.IsActive).HasDatabaseName("IX_countries_is_active");
        builder.HasQueryFilter(c => c.IsActive);

        // Concurrency token
        builder.Property(c => c.Version).HasColumnName("version")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();

        // Audit columns
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc")
            .HasDefaultValueSql("NOW()");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastModifiedUtc).HasColumnName("last_modified_utc")
            .HasDefaultValueSql("NOW()");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
    }
}
