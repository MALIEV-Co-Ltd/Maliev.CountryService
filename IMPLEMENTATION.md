# Country WebAPI Service - Implementation Roadmap

**Status**: Foundation setup complete (T001-T011) ✅
**Remaining**: T012-T154 (143 tasks)

This document provides step-by-step guidance for completing all remaining implementation tasks from `specs/001-country-service/tasks.md`.

---

## ✅ Completed Tasks (T001-T011)

- T001: Solution file exists
- T002-T004: Project files exist (Api, Data, Tests)
- T005: Project references configured in .sln
- T008-T010: NuGet packages configured in all .csproj files
- T011: `TreatWarningsAsErrors` enabled in all projects

---

## 📋 Remaining Setup Tasks (T012-T015)

### T012: Create appsettings.json

**File**: `Maliev.CountryService.Api/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "CountryServiceDbContext": "PLACEHOLDER_FROM_SECRETS"
  },
  "Redis": {
    "ConnectionString": "PLACEHOLDER_FROM_SECRETS"
  },
  "CacheOptions": {
    "FreshTtlMinutes": 15,
    "GracePeriodMinutes": 5
  },
  "RateLimiting": {
    "ReadEndpoints": {
      "PermitLimit": 100,
      "Window": "00:01:00"
    },
    "AdminEndpoints": {
      "PermitLimit": 20,
      "Window": "00:01:00"
    }
  },
  "JwtBearer": {
    "Authority": "PLACEHOLDER",
    "Audience": "country-service",
    "RequireHttpsMetadata": true
  }
}
```

### T013: Create appsettings.Development.json

**File**: `Maliev.CountryService.Api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "CountryServiceDbContext": "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "JwtBearer": {
    "Authority": "http://localhost:8080",
    "RequireHttpsMetadata": false
  }
}
```

### T014: Create docker-compose.test.yml

**File**: `docker-compose.test.yml` (repository root)

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:18
    container_name: country-service-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: country_service_app_db
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: country-service-redis
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

volumes:
  postgres_data:
```

### T015: Create Top50PopulousCountries.json

**File**: `Maliev.CountryService.Api/Configuration/Top50PopulousCountries.json`

```json
[
  "CN", "IN", "US", "ID", "PK", "NG", "BR", "BD", "RU", "MX",
  "JP", "ET", "PH", "EG", "VN", "CD", "TR", "IR", "DE", "TH",
  "GB", "FR", "IT", "TZ", "ZA", "MM", "KR", "CO", "ES", "KE",
  "AR", "DZ", "SD", "UG", "UA", "CA", "PL", "MA", "IQ", "AF",
  "PE", "MY", "SA", "UZ", "VE", "NP", "GH", "YE", "MZ", "AU"
]
```

---

## 🏗️ Phase 2: Foundational Tasks (T016-T048)

### Data Layer (T016-T025)

#### T016: Create Country Entity

**File**: `Maliev.CountryService.Data/Models/Country.cs`

```csharp
namespace Maliev.CountryService.Data.Models;

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

    // JSONB fields stored as JSON strings
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

    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
}
```

#### T017-T018: Create AuditLog and BulkImportJob Entities

Follow the same pattern from data-model.md for these entities.

#### T019-T021: Create FluentAPI Configurations

**File**: `Maliev.CountryService.Data/Configurations/CountryConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.CountryService.Data.Models;

namespace Maliev.CountryService.Data.Configurations;

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
        builder.HasIndex(c => c.Name).HasDatabaseName("IX_countries_name_gin");

        // Add remaining field configurations from data-model.md
        // JSONB columns:
        builder.Property(c => c.Timezones).HasColumnName("timezones").HasColumnType("jsonb");
        builder.Property(c => c.Currencies).HasColumnName("currencies").HasColumnType("jsonb");
        // ... etc

        builder.Property(c => c.Version).HasColumnName("version")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();

        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.HasIndex(c => c.IsActive);
    }
}
```

#### T022: Create CountryServiceDbContext

**File**: `Maliev.CountryService.Data/CountryServiceDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Maliev.CountryService.Data.Models;
using Maliev.CountryService.Data.Configurations;

namespace Maliev.CountryService.Data;

public class CountryServiceDbContext : DbContext
{
    public CountryServiceDbContext(DbContextOptions<CountryServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BulkImportJob> BulkImportJobs => Set<BulkImportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new BulkImportJobConfiguration());
    }
}
```

#### T023-T024: Migrations

```bash
# Create design-time factory first (T023)
# Then run migrations
dotnet ef migrations add InitialCreate --project Maliev.CountryService.Data --startup-project Maliev.CountryService.Api
```

### API Foundation (T026-T044)

#### T026: Create Program.cs

**File**: `Maliev.CountryService.Api/Program.cs`

```csharp
using Asp.Versioning;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Maliev.CountryService.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration (T026)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Google Secret Manager (T028)
var secretsPath = "/mnt/secrets";
if (Directory.Exists(secretsPath))
{
    builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
}

// DbContext (T027)
builder.Services.AddDbContext<CountryServiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CountryServiceDbContext")
        ?? throw new InvalidOperationException("CountryServiceDbContext connection string not found");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
    });
});

// Services
builder.Services.AddControllers();

// Memory Cache (T030 - CRITICAL: No SizeLimit per CLAUDE.md)
builder.Services.AddMemoryCache();

// Redis (T031)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// Health Checks (T029)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CountryServiceDbContext>(tags: new[] { "readiness" });

// API Versioning (T033)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// OpenAPI & Scalar (T034)
builder.Services.AddOpenApi();

// TODO: Add remaining service registrations from T036-T048
// - Middleware
// - JWT Authentication
// - Rate Limiting
// - Validators
// - Services
// - Background Services

var app = builder.Build();

// Middleware Pipeline (T035)
app.UseOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Country WebAPI Service");
});

app.UseHttpsRedirection();

// TODO: Add remaining middleware
// app.UseRateLimiter();
// app.UseAuthentication();
// app.UseAuthorization();

// Health Checks
app.MapGet("/countries/v1/liveness", () => "Healthy").AllowAnonymous();
app.MapHealthChecks("/countries/v1/readiness", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapControllers();

app.Run();
```

---

## 🎯 User Story Implementation Pattern

For each user story (Phases 3-8), follow this pattern:

### 1. Create DTOs
```csharp
// Example for US1 (T049-T051)
namespace Maliev.CountryService.Api.Models.Countries;

public record CountryResponse
{
    public long Id { get; init; }
    public string Iso2 { get; init; } = null!;
    public string Iso3 { get; init; } = null!;
    public string Name { get; init; } = null!;
    // ... all fields from Country entity
    public string Version { get; init; } = null!; // ETag from Guid
    public DateTime CreatedAtUtc { get; init; }
    public DateTime LastModifiedUtc { get; init; }
}
```

### 2. Create Service Interfaces
```csharp
// Example for US1 (T052)
namespace Maliev.CountryService.Api.Services;

public interface ICountryService
{
    Task<CountryResponse?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken ct = default);
    Task<CountryResponse?> GetByIso3Async(string iso3, CancellationToken ct = default);
    // ... more methods
}
```

### 3. Implement Services
```csharp
// Example for US1 (T053)
public class CountryService : ICountryService
{
    private readonly CountryServiceDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<CountryService> _logger;

    public async Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken ct)
    {
        var cacheKey = $"country:iso2:{iso2}";

        // Try cache first
        var cached = await _cache.GetAsync<CountryResponse>(cacheKey, ct);
        if (cached != null) return cached;

        // Query database
        var country = await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Iso2 == iso2 && c.IsActive, ct);

        if (country == null) return null;

        var response = MapToResponse(country);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), ct);

        return response;
    }

    private static CountryResponse MapToResponse(Country country)
    {
        return new CountryResponse
        {
            Id = country.Id,
            Iso2 = country.Iso2,
            // ... map all fields
            Version = Convert.ToBase64String(country.Version.ToByteArray())
        };
    }
}
```

### 4. Create Controllers
```csharp
// Example for US1 (T061-T067)
[ApiController]
[ApiVersion("1.0")]
[Route("countries/v{version:apiVersion}")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;

    [HttpGet("countries/iso2/{iso2}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByIso2(string iso2, CancellationToken ct)
    {
        var country = await _countryService.GetByIso2Async(iso2, ct);

        if (country == null)
            return NotFound(new { error = "CountryNotFound", message = $"Country with ISO2 code '{iso2}' not found" });

        Response.Headers.Add("ETag", $"\"{country.Version}\"");
        Response.Headers.Add("Last-Modified", country.LastModifiedUtc.ToString("R"));
        Response.Headers.Add("X-Cache", "HIT"); // Set based on cache status

        return Ok(country);
    }
}
```

---

## 🚀 Next Steps

1. **Complete Setup** (T012-T015): Create configuration files above
2. **Build Foundation** (T016-T048): Create all entities, configurations, DbContext, middleware, health checks
3. **Implement User Stories** (T049-T126): Follow pattern above for each story in priority order
4. **Polish** (T127-T154): Docker, CI/CD, documentation

### Quick Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run migrations
dotnet ef database update --project Maliev.CountryService.Data --startup-project Maliev.CountryService.Api

# Run tests
dotnet test

# Run API locally
cd Maliev.CountryService.Api
dotnet run
```

---

## 📚 Reference Documents

- **Plan**: `specs/001-country-service/plan.md`
- **Data Model**: `specs/001-country-service/data-model.md`
- **API Contracts**: `specs/001-country-service/contracts/openapi.yaml`
- **Research**: `specs/001-country-service/research.md`
- **Tasks**: `specs/001-country-service/tasks.md`
- **Quickstart**: `specs/001-country-service/quickstart.md`

All implementation details are in these documents. Follow them exactly for constitutional compliance.

---

**Total Progress**: 11/154 tasks complete (7%)
**Next Milestone**: Complete foundational phase (T016-T048) to unblock all user stories
