# Data Model: Country WebAPI Service

**Feature**: Country WebAPI Service
**Branch**: `001-country-service`
**Date**: 2025-11-01
**Database**: PostgreSQL 18 (`country_service_app_db`)

## Overview

This document defines the complete data model for the Country WebAPI Service, including entity schemas, relationships, indexes, and migration strategy. All entities use Entity Framework Core 9.0 with Fluent API configuration.

---

## Entity Schemas

### Country Entity

**Table Name**: `countries`

Primary entity representing a country with all geographical, administrative, and metadata attributes.

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| `id` | `bigint` | NO | IDENTITY | PRIMARY KEY |
| `iso2` | `varchar(2)` | NO | - | UNIQUE INDEX |
| `iso3` | `varchar(3)` | NO | - | UNIQUE INDEX |
| `name` | `varchar(100)` | NO | - | GIN INDEX (full-text) |
| `official_name` | `varchar(200)` | YES | NULL | - |
| `numeric_code` | `varchar(3)` | YES | NULL | - |
| `capital` | `varchar(100)` | YES | NULL | - |
| `region` | `varchar(50)` | YES | NULL | - |
| `subregion` | `varchar(50)` | YES | NULL | - |
| `latitude` | `decimal(10,8)` | YES | NULL | CHECK (-90 to 90) |
| `longitude` | `decimal(11,8)` | YES | NULL | CHECK (-180 to 180) |
| `demonym` | `varchar(50)` | YES | NULL | - |
| `area_km2` | `decimal(15,2)` | YES | NULL | CHECK (>= 0) |
| `population` | `bigint` | YES | NULL | CHECK (>= 0) |
| `gini_coefficient` | `decimal(4,2)` | YES | NULL | CHECK (0 to 100) |
| `timezones` | `jsonb` | NO | '[]' | Array of timezone strings |
| `borders` | `jsonb` | NO | '[]' | Array of ISO3 codes |
| `calling_codes` | `jsonb` | NO | '[]' | Array of calling codes |
| `top_level_domains` | `jsonb` | NO | '[]' | Array of TLDs |
| `currencies` | `jsonb` | NO | '{}' | JSON object |
| `languages` | `jsonb` | NO | '{}' | JSON object |
| `translations` | `jsonb` | NO | '{}' | JSON object |
| `flags` | `jsonb` | NO | '{}' | JSON object (svg, png URLs) |
| `coat_of_arms` | `jsonb` | YES | NULL | JSON object (svg, png URLs) |
| `independent` | `boolean` | NO | false | - |
| `un_member` | `boolean` | NO | false | - |
| `landlocked` | `boolean` | NO | false | - |
| `is_active` | `boolean` | NO | true | Soft delete flag |
| `version` | `uuid` | NO | gen_random_uuid() | Concurrency token |
| `created_at_utc` | `timestamp` | NO | NOW() | - |
| `last_modified_utc` | `timestamp` | NO | NOW() | - |

**Indexes**:
- `PK_countries` on `id` (clustered)
- `UQ_countries_iso2` on `iso2` UNIQUE
- `UQ_countries_iso3` on `iso3` UNIQUE
- `IX_countries_name_gin` on `name` using GIN (for full-text search)
- `IX_countries_region` on `region` (filter queries)
- `IX_countries_is_active` on `is_active` (soft delete filtering)

**C# Entity Class**:
```csharp
public class Country
{
    public long Id { get; set; }
    public string Iso2 { get; set; } = null!;
    public string Iso3 { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? OfficialName { get; set; }
    public string? NumericCode { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }
    public string? Subregion { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Demonym { get; set; }
    public decimal? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public decimal? GiniCoefficient { get; set; }

    // JSONB columns mapped as strings (serialized/deserialized in service layer)
    public string Timezones { get; set; } = "[]";
    public string Borders { get; set; } = "[]";
    public string CallingCodes { get; set; } = "[]";
    public string TopLevelDomains { get; set; } = "[]";
    public string Currencies { get; set; } = "{}";
    public string Languages { get; set; } = "{}";
    public string Translations { get; set; } = "{}";
    public string Flags { get; set; } = "{}";
    public string? CoatOfArms { get; set; }

    public bool Independent { get; set; }
    public bool UnMember { get; set; }
    public bool Landlocked { get; set; }
    public bool IsActive { get; set; } = true;

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
}
```

---

### AuditLog Entity

**Table Name**: `audit_logs`

Immutable audit trail for all country modification operations (create, update, delete, bulk import).

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| `id` | `bigint` | NO | IDENTITY | PRIMARY KEY |
| `country_id` | `bigint` | NO | - | INDEX |
| `operation` | `varchar(20)` | NO | - | CHECK (Create, Update, Delete, BulkImport) |
| `user_id` | `varchar(100)` | NO | - | INDEX (from JWT `sub` claim) |
| `user_email` | `varchar(255)` | YES | NULL | - |
| `user_roles` | `jsonb` | NO | '[]' | Array of role strings |
| `before_snapshot` | `jsonb` | YES | NULL | Full entity state before change |
| `after_snapshot` | `jsonb` | NO | - | Full entity state after change |
| `changed_fields` | `jsonb` | NO | '[]' | Array of field names |
| `ip_address` | `varchar(45)` | YES | NULL | IPv4/IPv6 |
| `user_agent` | `varchar(500)` | YES | NULL | - |
| `correlation_id` | `uuid` | YES | NULL | INDEX (request tracing) |
| `created_at_utc` | `timestamp` | NO | NOW() | - |

**Indexes**:
- `PK_audit_logs` on `id` (clustered)
- `IX_audit_logs_country_id` on `country_id` (FK lookup)
- `IX_audit_logs_user_id` on `user_id` (user activity queries)
- `IX_audit_logs_correlation_id` on `correlation_id` (request tracing)
- `IX_audit_logs_created_at_utc` on `created_at_utc` (time-range queries, retention)

**Retention Policy**:
- Partition by month (PostgreSQL table partitioning)
- Drop partitions older than 24 months automatically
- Implemented via scheduled job or pg_cron extension

**C# Entity Class**:
```csharp
public class AuditLog
{
    public long Id { get; set; }
    public long CountryId { get; set; }
    public string Operation { get; set; } = null!; // Create, Update, Delete, BulkImport
    public string UserId { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string UserRoles { get; set; } = "[]";
    public string? BeforeSnapshot { get; set; }
    public string AfterSnapshot { get; set; } = null!;
    public string ChangedFields { get; set; } = "[]";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

---

### BulkImportJob Entity

**Table Name**: `bulk_import_jobs`

Tracks bulk import operations with status, validation errors, and processing metrics.

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| `id` | `bigint` | NO | IDENTITY | PRIMARY KEY |
| `status` | `varchar(20)` | NO | 'Pending' | CHECK (Pending, Validating, Validated, ValidationFailed, Processing, Completed, Failed) |
| `total_records` | `int` | NO | - | CHECK (> 0) |
| `processed_records` | `int` | NO | 0 | CHECK (>= 0) |
| `failed_records` | `int` | NO | 0 | CHECK (>= 0) |
| `validation_errors` | `jsonb` | NO | '[]' | Array of error objects |
| `error_message` | `text` | YES | NULL | - |
| `user_id` | `varchar(100)` | NO | - | INDEX |
| `user_email` | `varchar(255)` | YES | NULL | - |
| `ip_address` | `varchar(45)` | YES | NULL | - |
| `correlation_id` | `uuid` | YES | NULL | INDEX |
| `created_at_utc` | `timestamp` | NO | NOW() | - |
| `started_at_utc` | `timestamp` | YES | NULL | - |
| `completed_at_utc` | `timestamp` | YES | NULL | - |
| `duration_ms` | `bigint` | YES | NULL | Computed: completed - started |

**Indexes**:
- `PK_bulk_import_jobs` on `id` (clustered)
- `IX_bulk_import_jobs_user_id` on `user_id` (user query)
- `IX_bulk_import_jobs_status` on `status` (status filtering)
- `IX_bulk_import_jobs_created_at_utc` on `created_at_utc` DESC (recent jobs)
- `IX_bulk_import_jobs_correlation_id` on `correlation_id` (request tracing)

**Status State Machine**:
```
Pending → Validating → Validated → Processing → Completed
                    ↓                        ↓
                ValidationFailed            Failed
```

**C# Entity Class**:
```csharp
public class BulkImportJob
{
    public long Id { get; set; }
    public string Status { get; set; } = "Pending";
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public string ValidationErrors { get; set; } = "[]";
    public string? ErrorMessage { get; set; }
    public string UserId { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Computed property
    public long? DurationMs =>
        CompletedAtUtc.HasValue && StartedAtUtc.HasValue
            ? (long)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalMilliseconds
            : null;
}
```

---

## Entity Relationships

```
┌─────────────────┐
│    Country      │
│  (countries)    │
│                 │
│  PK: id         │
│  UQ: iso2       │
│  UQ: iso3       │
└────────┬────────┘
         │
         │ 1
         │
         │ N
         │
┌────────▼────────┐
│   AuditLog      │
│ (audit_logs)    │
│                 │
│ PK: id          │
│ FK: country_id  │ (No foreign key constraint - immutable audit)
└─────────────────┘

┌──────────────────┐
│ BulkImportJob    │
│(bulk_import_jobs)│
│                  │
│ PK: id           │
└──────────────────┘
(No FK to Country - tracks validation/processing only)
```

**Relationship Notes**:
- `AuditLog.country_id` does NOT have a foreign key constraint to allow immutable audit records even after hard deletes
- `BulkImportJob` is standalone - tracks import operations without direct reference to created countries

---

## EF Core Fluent API Configurations

### CountryConfiguration.cs

```csharp
public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.Iso2).HasColumnName("iso2").HasMaxLength(2).IsRequired();
        builder.HasIndex(c => c.Iso2).IsUnique().HasDatabaseName("UQ_countries_iso2");

        builder.Property(c => c.Iso3).HasColumnName("iso3").HasMaxLength(3).IsRequired();
        builder.HasIndex(c => c.Iso3).IsUnique().HasDatabaseName("UQ_countries_iso3");

        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).HasDatabaseName("IX_countries_name_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.Property(c => c.OfficialName).HasColumnName("official_name").HasMaxLength(200);
        builder.Property(c => c.NumericCode).HasColumnName("numeric_code").HasMaxLength(3);
        builder.Property(c => c.Capital).HasColumnName("capital").HasMaxLength(100);

        builder.Property(c => c.Region).HasColumnName("region").HasMaxLength(50);
        builder.HasIndex(c => c.Region).HasDatabaseName("IX_countries_region");

        builder.Property(c => c.Subregion).HasColumnName("subregion").HasMaxLength(50);

        builder.Property(c => c.Latitude).HasColumnName("latitude").HasPrecision(10, 8);
        builder.Property(c => c.Longitude).HasColumnName("longitude").HasPrecision(11, 8);

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

        builder.Property(c => c.Independent).HasColumnName("independent").HasDefaultValue(false);
        builder.Property(c => c.UnMember).HasColumnName("un_member").HasDefaultValue(false);
        builder.Property(c => c.Landlocked).HasColumnName("landlocked").HasDefaultValue(false);

        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasIndex(c => c.IsActive).HasDatabaseName("IX_countries_is_active");

        builder.Property(c => c.Version).HasColumnName("version")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();

        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc")
            .HasDefaultValueSql("NOW()");
        builder.Property(c => c.LastModifiedUtc).HasColumnName("last_modified_utc")
            .HasDefaultValueSql("NOW()");
    }
}
```

### AuditLogConfiguration.cs

```csharp
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(a => a.CountryId).HasColumnName("country_id").IsRequired();
        builder.HasIndex(a => a.CountryId).HasDatabaseName("IX_audit_logs_country_id");
        // NO foreign key constraint - allow audit after hard delete

        builder.Property(a => a.Operation).HasColumnName("operation").HasMaxLength(20).IsRequired();
        builder.Property(a => a.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_audit_logs_user_id");

        builder.Property(a => a.UserEmail).HasColumnName("user_email").HasMaxLength(255);
        builder.Property(a => a.UserRoles).HasColumnName("user_roles").HasColumnType("jsonb").HasDefaultValue("[]");

        builder.Property(a => a.BeforeSnapshot).HasColumnName("before_snapshot").HasColumnType("jsonb");
        builder.Property(a => a.AfterSnapshot).HasColumnName("after_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.ChangedFields).HasColumnName("changed_fields").HasColumnType("jsonb").HasDefaultValue("[]");

        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);

        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id");
        builder.HasIndex(a => a.CorrelationId).HasDatabaseName("IX_audit_logs_correlation_id");

        builder.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.HasIndex(a => a.CreatedAtUtc).HasDatabaseName("IX_audit_logs_created_at_utc");
    }
}
```

### BulkImportJobConfiguration.cs

```csharp
public class BulkImportJobConfiguration : IEntityTypeConfiguration<BulkImportJob>
{
    public void Configure(EntityTypeBuilder<BulkImportJob> builder)
    {
        builder.ToTable("bulk_import_jobs");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(b => b.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending");
        builder.HasIndex(b => b.Status).HasDatabaseName("IX_bulk_import_jobs_status");

        builder.Property(b => b.TotalRecords).HasColumnName("total_records").IsRequired();
        builder.Property(b => b.ProcessedRecords).HasColumnName("processed_records").HasDefaultValue(0);
        builder.Property(b => b.FailedRecords).HasColumnName("failed_records").HasDefaultValue(0);

        builder.Property(b => b.ValidationErrors).HasColumnName("validation_errors").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(b => b.ErrorMessage).HasColumnName("error_message").HasColumnType("text");

        builder.Property(b => b.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
        builder.HasIndex(b => b.UserId).HasDatabaseName("IX_bulk_import_jobs_user_id");

        builder.Property(b => b.UserEmail).HasColumnName("user_email").HasMaxLength(255);
        builder.Property(b => b.IpAddress).HasColumnName("ip_address").HasMaxLength(45);

        builder.Property(b => b.CorrelationId).HasColumnName("correlation_id");
        builder.HasIndex(b => b.CorrelationId).HasDatabaseName("IX_bulk_import_jobs_correlation_id");

        builder.Property(b => b.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("NOW()");
        builder.HasIndex(b => b.CreatedAtUtc).HasDatabaseName("IX_bulk_import_jobs_created_at_utc").IsDescending();

        builder.Property(b => b.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(b => b.CompletedAtUtc).HasColumnName("completed_at_utc");

        builder.Ignore(b => b.DurationMs); // Computed property
    }
}
```

---

## Migration Strategy

### Initial Migration: `20251101_InitialCreate`

```bash
dotnet ef migrations add InitialCreate --project Maliev.CountryService.Data --output-dir Migrations
```

**Migration Contents**:
1. Create `countries` table with all columns, indexes, constraints
2. Create `audit_logs` table with indexes (NO FK constraint)
3. Create `bulk_import_jobs` table with indexes
4. Enable `pg_trgm` extension for GIN full-text search:
   ```sql
   CREATE EXTENSION IF NOT EXISTS pg_trgm;
   ```

### Seed Data Migration: `20251101_SeedTop50Countries`

Manual data migration to pre-populate top 50 most populous countries for cache warming.

**Seed Data Source**:
- `Maliev.CountryService.Api/Configuration/Top50PopulousCountries.json`
- Contains minimal data: `iso2`, `iso3`, `name`, `population`

**Migration Script**:
```sql
INSERT INTO countries (iso2, iso3, name, population, is_active, created_at_utc, last_modified_utc)
VALUES
  ('CN', 'CHN', 'China', 1400000000, true, NOW(), NOW()),
  ('IN', 'IND', 'India', 1390000000, true, NOW(), NOW()),
  -- ... (48 more countries)
ON CONFLICT (iso2) DO NOTHING;
```

### Future Migrations

All schema changes will follow EF Core migration workflow:
1. Modify entity classes
2. Generate migration: `dotnet ef migrations add <MigrationName>`
3. Review generated SQL
4. Test migration on local PostgreSQL
5. Apply to dev/staging/production via CI/CD pipeline

**Zero-Downtime Migration Strategy**:
- Additive changes: Deploy immediately (new columns nullable)
- Destructive changes: Blue-green deployment with backward-compatible transition period
- Index creation: Use `CONCURRENTLY` option for production

---

## Database Initialization

### DbContext Registration

```csharp
builder.Services.AddDbContext<CountryServiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CountryServiceDbContext")
        ?? throw new InvalidOperationException("CountryServiceDbContext connection string not found");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

### Design-Time DbContext Factory

```csharp
public class CountryServiceDbContextFactory : IDesignTimeDbContextFactory<CountryServiceDbContext>
{
    public CountryServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CountryServiceDbContext>();

        // Read from environment variable for migration commands
        var connectionString = Environment.GetEnvironmentVariable("CountryServiceDbContext")
            ?? "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;";

        optionsBuilder.UseNpgsql(connectionString);

        return new CountryServiceDbContext(optionsBuilder.Options);
    }
}
```

---

## Performance Considerations

### Index Selectivity
- `iso2` and `iso3` unique indexes: 100% selectivity (primary lookups)
- `name` GIN index: Full-text search for country name queries
- `region` index: Medium selectivity (~20 unique regions), good for filtering
- `is_active` index: Low selectivity but critical for soft delete filtering

### JSONB Indexing (Future Optimization)
If queries on JSONB fields become performance bottlenecks, consider:
```sql
CREATE INDEX IX_countries_currencies_gin ON countries USING GIN (currencies);
CREATE INDEX IX_countries_languages_gin ON countries USING GIN (languages);
```

### Query Performance Targets
- Single country lookup by ID/ISO2/ISO3: <5ms (index-only scan)
- List all active countries: <20ms (250 rows, index scan on `is_active`)
- Full-text search by name: <50ms (GIN index trigram search)

---

## Audit Log Retention Implementation

### PostgreSQL Partitioning (Future Enhancement)

```sql
-- Convert audit_logs to partitioned table
CREATE TABLE audit_logs_partitioned (LIKE audit_logs INCLUDING ALL)
PARTITION BY RANGE (created_at_utc);

-- Create monthly partitions
CREATE TABLE audit_logs_2025_01 PARTITION OF audit_logs_partitioned
FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');

-- Automated partition management via pg_cron
SELECT cron.schedule('drop_old_audit_partitions', '0 0 1 * *', $$
  SELECT drop_old_partitions('audit_logs_partitioned', '24 months');
$$);
```

**Note**: Initial implementation uses single table with manual cleanup query. Partitioning can be added later based on volume.

---

## Testing Data Model

### Unit Tests (EF Core Configuration)
- Validate entity configurations produce expected SQL schema
- Test concurrency token behavior with `DbUpdateConcurrencyException`
- Validate JSONB serialization/deserialization

### Integration Tests (Real PostgreSQL)
- Test all indexes are created correctly
- Test unique constraints prevent duplicates
- Test soft delete queries filter correctly
- Test audit log creation on mutations
- Test bulk import validation with database duplicate detection

---

## Summary

Data model supports all spec requirements:
- ✅ Optimistic concurrency via `version` UUID field
- ✅ Soft delete via `is_active` boolean
- ✅ Immutable audit logs with 24-month retention
- ✅ Bulk import job tracking with validation errors
- ✅ JSONB for flexible nested data (timezones, currencies, etc.)
- ✅ Full-text search on country name
- ✅ Efficient indexes for <50ms read latency

**Next**: Generate API contracts (OpenAPI specification)
