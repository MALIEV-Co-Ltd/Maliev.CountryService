# Data Model: Country WebAPI Service

**Branch**: `001-country-service` | **Spec**: [spec.md](spec.md)
**Database**: PostgreSQL 16+
**ORM**: Entity Framework Core 10.0

## Schemas

### Table: `Countries`

Canonical storage for country data.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `bigint` | PK, Identity | Internal unique identifier |
| `Iso2` | `char(2)` | UK, Not Null | ISO 3166-1 alpha-2 code (e.g., "US") |
| `Iso3` | `char(3)` | UK, Nullable | ISO 3166-1 alpha-3 code (e.g., "USA") |
| `Name` | `varchar(100)` | Not Null | Common English name |
| `OfficialName` | `varchar(200)` | Nullable | Official English name |
| `NumericCode` | `char(3)` | Nullable | ISO 3166-1 numeric code |
| `Region` | `varchar(50)` | Nullable | Continent/Region |
| `Subregion` | `varchar(50)` | Nullable | Specific subregion |
| `Population` | `bigint` | Nullable | Estimated population |
| `AreaKm2` | `double` | Nullable | Land area in sq km |
| `Timezones` | `jsonb` | Not Null | Array of IANA timezone strings |
| `Currencies` | `jsonb` | Nullable | Object describing currencies |
| `Languages` | `jsonb` | Nullable | Object describing languages |
| `Translations` | `jsonb` | Nullable | Object with localized names |
| `Flags` | `jsonb` | Nullable | URLs for flag images |
| `IsActive` | `boolean` | Not Null, Default `true` | Soft-delete flag |
| `Version` | `uuid` | Not Null | Optimistic concurrency token |
| `CreatedAtUtc` | `timestamp` | Not Null | Creation timestamp |
| `LastModifiedUtc` | `timestamp` | Not Null | Update timestamp |
| `CreatedBy` | `varchar(100)` | Not Null | User ID of creator |
| `UpdatedBy` | `varchar(100)` | Not Null | User ID of last modifier |

**Indexes:**
- Unique Index on `Iso2`
- Unique Index on `Iso3` (where not null)
- GIN Index on `Name` (for search)
- Index on `Region`, `Subregion` (for filtering)
- Index on `IsActive`

### Table: `AuditLogs`

History of all mutations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `bigint` | PK, Identity | Unique identifier |
| `CountryId` | `bigint` | FK(`Countries.Id`) | Reference to affected country |
| `Action` | `varchar(20)` | Not Null | CREATE, UPDATE, DELETE, HARD_DELETE |
| `UserId` | `varchar(100)` | Not Null | User performing action |
| `TimestampUtc` | `timestamp` | Not Null | When action occurred |
| `Changes` | `jsonb` | Nullable | Diff or snapshot of changes |
| `IpAddress` | `varchar(45)` | Nullable | Request source IP |

**Indexes:**
- Index on `CountryId`
- Index on `TimestampUtc`

### Table: `BulkImportJobs`

Tracking for asynchronous import jobs.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `bigint` | PK, Identity | Job identifier |
| `Status` | `varchar(20)` | Not Null | Pending, Validating, Processing, Completed, Failed |
| `TotalRecords` | `int` | Not Null | Count of records in batch |
| `ProcessedRecords` | `int` | Not Null | Successfully processed count |
| `FailedRecords` | `int` | Not Null | Validation failure count |
| `Errors` | `jsonb` | Nullable | Array of validation errors |
| `CreatedAtUtc` | `timestamp` | Not Null | Submission time |
| `CompletedAtUtc` | `timestamp` | Nullable | Completion time |
| `CreatedBy` | `varchar(100)` | Not Null | Submitter ID |

## Entity Classes (Preview)

```csharp
public class Country
{
    public long Id { get; set; }
    
    [Required, StringLength(2)]
    public string Iso2 { get; set; } = string.Empty;
    
    [StringLength(3)]
    public string? Iso3 { get; set; }
    
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [ConcurrencyCheck]
    public Guid Version { get; set; }
    
    // ... other properties
}
```