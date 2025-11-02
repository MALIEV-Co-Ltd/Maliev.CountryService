# Phase 0: Research & Technology Decisions

**Feature**: Country WebAPI Service
**Branch**: `001-country-service`
**Date**: 2025-11-01
**Input**: [spec.md](./spec.md), [plan.md](./plan.md)

## Overview

This document captures technology research and architectural decisions for implementing the Country WebAPI Service. All decisions align with Maliev microservices standards and the project constitution.

---

## Research Area 1: Cache Warming Strategy

### Context
The service must pre-load the top 50 most populous countries into in-memory cache on startup to ensure sub-50ms p95 latency for the most frequently accessed data.

### Implementation Pattern

**Static Configuration Approach**:
- Maintain `Top50PopulousCountries.json` in `Maliev.CountryService.Api/Configuration/`
- Contains array of ISO2 codes: `["CN", "IN", "US", "ID", "PK", ...]`
- Loaded via `IConfiguration` and injected into `CacheWarmingService`

**Background Service Implementation**:
```csharp
public class CacheWarmingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly string[] _top50Iso2Codes;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 5 seconds after startup to allow DB connections to initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var scope = _scopeFactory.CreateScope();
        var countryService = scope.ServiceProvider.GetRequiredService<ICountryService>();

        foreach (var iso2 in _top50Iso2Codes)
        {
            try
            {
                await countryService.GetByIso2Async(iso2, stoppingToken);
                _logger.LogInformation("Cached country {Iso2}", iso2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache country {Iso2}", iso2);
            }
        }
    }
}
```

**Performance Impact**:
- Warm-up time: ~50-100ms total (1-2ms per country × 50 countries)
- Cache memory: ~50KB for 50 country records
- No blocking of application startup (runs in background)

**Metrics**:
- `country_cache_warming_duration_seconds` histogram
- `country_cache_warming_success_total` counter
- `country_cache_warming_failure_total` counter

**Decision**: Use static JSON configuration with background service warming. Simple, testable, no external dependencies.

---

## Research Area 2: Optimistic Concurrency with ETag

### Context
Multiple administrators may attempt to update the same country record. The system must prevent lost updates using HTTP ETag-based optimistic concurrency control.

### Implementation Pattern

**Database Version Field**:
```csharp
public class Country
{
    public long Id { get; set; }
    public string Iso2 { get; set; } = null!;
    // ... other fields

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime LastModifiedUtc { get; set; }
}
```

**ETag Generation**:
- ETag = Base64-encoded SHA256 hash of `Version` field
- Generated in `CountryResponse` DTO mapping
- Returned in `ETag` response header for GET requests

**Validation Pattern**:
```csharp
public async Task<CountryResponse> UpdateAsync(long id, UpdateCountryRequest request, string? ifMatch)
{
    var country = await _context.Countries.FindAsync(id);

    // Validate ETag if provided
    if (!string.IsNullOrEmpty(ifMatch))
    {
        var expectedETag = GenerateETag(country.Version);
        if (ifMatch != expectedETag)
        {
            throw new PreconditionFailedException("Version conflict detected");
        }
    }

    // Apply updates
    country.Name = request.Name;
    country.Version = Guid.NewGuid(); // New version on every update
    country.LastModifiedUtc = DateTime.UtcNow;

    await _context.SaveChangesAsync(); // DbUpdateConcurrencyException if Version changed

    return MapToResponse(country);
}
```

**HTTP Status Codes**:
- `412 Precondition Failed`: If-Match header doesn't match current ETag
- `409 Conflict`: Concurrent update detected by EF Core (DbUpdateConcurrencyException)

**Client Workflow**:
1. Client fetches country: `GET /countries/v1/{id}` → receives ETag
2. Client updates country: `PUT /countries/v1/{id}` with `If-Match: <etag>`
3. Server validates ETag before applying changes

**Decision**: Use Guid-based version field with SHA256 ETag generation. Aligns with HTTP RFC 7232, works seamlessly with EF Core concurrency checks.

---

## Research Area 3: Bulk Import Atomic Validation

### Context
Bulk import operations must validate ALL records before applying ANY changes. A single invalid record should reject the entire batch with detailed error reporting.

### Implementation Pattern

**Two-Phase Processing**:

**Phase 1: Validation Only**
```csharp
public async Task<BulkImportJob> ValidateImportAsync(BulkImportRequest request)
{
    var errors = new List<ValidationError>();
    var seenIso2Codes = new HashSet<string>();
    var seenIso3Codes = new HashSet<string>();

    // Validate each record
    for (int i = 0; i < request.Countries.Count; i++)
    {
        var country = request.Countries[i];
        var rowNumber = i + 1;

        // Check duplicates within batch
        if (!seenIso2Codes.Add(country.Iso2))
            errors.Add(new ValidationError(rowNumber, "Iso2", $"Duplicate ISO2 code {country.Iso2} in batch"));

        if (!seenIso3Codes.Add(country.Iso3))
            errors.Add(new ValidationError(rowNumber, "Iso3", $"Duplicate ISO3 code {country.Iso3} in batch"));

        // Validate against FluentValidation rules
        var validator = new CreateCountryRequestValidator();
        var result = await validator.ValidateAsync(country);

        foreach (var failure in result.Errors)
        {
            errors.Add(new ValidationError(rowNumber, failure.PropertyName, failure.ErrorMessage));
        }
    }

    // Check database duplicates
    var allIso2 = request.Countries.Select(c => c.Iso2).ToArray();
    var existingIso2 = await _context.Countries
        .Where(c => allIso2.Contains(c.Iso2))
        .Select(c => c.Iso2)
        .ToListAsync();

    foreach (var duplicate in existingIso2)
    {
        var rowNumber = request.Countries.FindIndex(c => c.Iso2 == duplicate) + 1;
        errors.Add(new ValidationError(rowNumber, "Iso2", $"ISO2 code {duplicate} already exists in database"));
    }

    return new BulkImportJob
    {
        Status = errors.Any() ? "ValidationFailed" : "Validated",
        TotalRecords = request.Countries.Count,
        ValidationErrors = errors
    };
}
```

**Phase 2: Atomic Transaction**
```csharp
public async Task ProcessImportAsync(long jobId)
{
    var job = await _context.BulkImportJobs.FindAsync(jobId);

    if (job.Status != "Validated")
        throw new InvalidOperationException("Job must be validated before processing");

    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        foreach (var countryData in job.CountriesData)
        {
            var country = new Country
            {
                Iso2 = countryData.Iso2,
                // ... map all fields
            };
            _context.Countries.Add(country);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        job.Status = "Completed";
        job.ProcessedRecords = job.TotalRecords;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        job.Status = "Failed";
        job.ErrorMessage = ex.Message;
    }

    await _context.SaveChangesAsync();
}
```

**Error Response Format**:
```json
{
  "jobId": 123,
  "status": "ValidationFailed",
  "totalRecords": 250,
  "errors": [
    {
      "rowNumber": 5,
      "field": "Iso2",
      "message": "Duplicate ISO2 code 'US' in batch"
    },
    {
      "rowNumber": 47,
      "field": "Name",
      "message": "Country name cannot exceed 100 characters"
    }
  ]
}
```

**Decision**: Use two-phase validation with in-memory duplicate detection and database transaction for atomic commits. Provides fast feedback and guarantees all-or-nothing semantics.

---

## Research Area 4: Stale-While-Revalidate Pattern

### Context
Redis cache failures should not impact service availability. Implement graceful degradation with stale cache tolerance and background refresh.

### Implementation Pattern

**Redis Cache Entry Structure**:
```csharp
public class CachedCountry
{
    public CountryResponse Data { get; set; } = null!;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime StaleAt { get; set; } // ExpiresAt + grace period
}
```

**Cache Read Pattern**:
```csharp
public async Task<CountryResponse?> GetFromCacheAsync(string key)
{
    var cached = await _redis.StringGetAsync(key);

    if (cached.IsNullOrEmpty)
        return null;

    var entry = JsonSerializer.Deserialize<CachedCountry>(cached!);
    var now = DateTime.UtcNow;

    // Fresh cache hit
    if (now < entry.ExpiresAt)
    {
        _metrics.CacheHits.Inc();
        return entry.Data;
    }

    // Stale but within grace period
    if (now < entry.StaleAt)
    {
        _metrics.CacheStaleHits.Inc();

        // Trigger background refresh (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshCacheAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background cache refresh failed for {Key}", key);
            }
        });

        // Return stale data immediately
        return entry.Data with { IsStale = true };
    }

    // Too stale - discard
    _metrics.CacheMisses.Inc();
    return null;
}
```

**TTL Configuration**:
- **Fresh TTL**: 15 minutes (configurable via `CacheOptions:FreshTtlMinutes`)
- **Grace Period**: 5 minutes (configurable via `CacheOptions:GracePeriodMinutes`)
- **Total Max Age**: 20 minutes (fresh + grace)

**X-Cache Headers**:
```csharp
response.Headers.Add("X-Cache", entry.IsStale ? "STALE" : "HIT");
response.Headers.Add("X-Cache-Age", (now - entry.CachedAt).TotalSeconds.ToString("F0"));
```

**Metrics**:
- `country_cache_hits_total{type="fresh"}` - Cache hit within TTL
- `country_cache_hits_total{type="stale"}` - Cache hit in grace period
- `country_cache_misses_total` - Cache miss or too stale
- `country_cache_refresh_total{result="success|failure"}` - Background refresh attempts

**Decision**: Implement 15-minute fresh TTL with 5-minute grace period. Balances freshness with availability during Redis failures.

---

## Research Area 5: Circuit Breaker for Redis

### Context
Redis outages should not cascade to database overload. Implement circuit breaker to fail fast and fallback to in-memory cache.

### Implementation Pattern

**Polly v8 Circuit Breaker Configuration**:
```csharp
services.AddResiliencePipeline("redis-cache", builder =>
{
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,           // Open if 50% of requests fail
        MinimumThroughput = 10,       // Require 10 requests in sampling window
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(60),
        OnOpened = args =>
        {
            _logger.LogError("Redis circuit breaker OPENED - falling back to in-memory cache");
            _metrics.CircuitBreakerState.Set(1); // 1 = Open
            return ValueTask.CompletedTask;
        },
        OnClosed = args =>
        {
            _logger.LogInformation("Redis circuit breaker CLOSED - Redis healthy");
            _metrics.CircuitBreakerState.Set(0); // 0 = Closed
            return ValueTask.CompletedTask;
        },
        OnHalfOpened = args =>
        {
            _logger.LogInformation("Redis circuit breaker HALF-OPEN - probing");
            _metrics.CircuitBreakerState.Set(2); // 2 = Half-Open
            return ValueTask.CompletedTask;
        }
    });
});
```

**Fallback Pattern**:
```csharp
public async Task<CountryResponse?> GetAsync(string key)
{
    try
    {
        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            return await _redis.StringGetAsync(key);
        });
    }
    catch (BrokenCircuitException)
    {
        _logger.LogWarning("Redis circuit breaker open - using in-memory cache fallback");
        return _memoryCache.Get<CountryResponse>(key);
    }
}
```

**Write-Through Strategy During Outage**:
- Continue writing to in-memory cache
- Log Redis write failures but don't throw
- When circuit closes, cache will gradually repopulate from DB reads

**Health Check Integration**:
```csharp
public class RedisHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            await _redis.PingAsync();

            if (_circuitBreaker.IsOpen)
                return HealthCheckResult.Degraded("Redis available but circuit breaker open");

            return HealthCheckResult.Healthy("Redis operational");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis unavailable", ex);
        }
    }
}
```

**Metrics**:
- `country_redis_circuit_breaker_state{state="open|closed|half_open"}` gauge
- `country_redis_failures_total` counter
- `country_cache_fallback_total` counter (in-memory cache usage during outage)

**Decision**: Use Polly v8 circuit breaker with 50% failure threshold over 30-second window, 60-second break duration. Fallback to in-memory cache during outages.

---

## Research Area 6: Rate Limiting Partitioning

### Context
Rate limits must be enforced per authenticated user for admin endpoints and per IP for anonymous read endpoints.

### Implementation Pattern

**ASP.NET Core 9.0 Rate Limiting**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    // Read endpoints - partition by IP
    options.AddFixedWindowLimiter("read-endpoints", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    }).WithPartitionedKeyProvider<HttpContext>(context =>
    {
        // Use X-Forwarded-For if behind proxy, otherwise remote IP
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        });
    });

    // Admin endpoints - partition by user ID from JWT
    options.AddFixedWindowLimiter("admin-endpoints", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
    }).WithPartitionedKeyProvider<HttpContext>(context =>
    {
        // Extract user ID from JWT claims
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1)
        });
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? retry.TotalSeconds
            : 60;

        context.HttpContext.Response.Headers.Add("Retry-After", retryAfter.ToString("F0"));

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RateLimitExceeded",
            message = "Too many requests. Please try again later.",
            retryAfterSeconds = retryAfter
        }, ct);
    };
});
```

**Controller Attributes**:
```csharp
[ApiController]
[Route("countries/v1")]
public class CountriesController : ControllerBase
{
    [HttpGet("{id}")]
    [AllowAnonymous]
    [EnableRateLimiting("read-endpoints")]
    public async Task<IActionResult> GetById(long id) { }

    [HttpPost]
    [Authorize(Roles = "CountryAdmin")]
    [EnableRateLimiting("admin-endpoints")]
    public async Task<IActionResult> Create([FromBody] CreateCountryRequest request) { }
}
```

**Quota Design**:
- **Read endpoints**: 100 requests/minute per IP
- **Admin endpoints**: 20 requests/minute per authenticated user
- **Queue limit**: 10 queued requests before immediate rejection
- **Retry-After header**: Seconds until window reset

**Metrics**:
- `country_rate_limit_rejected_total{policy="read|admin"}` counter
- `country_rate_limit_requests_total{policy="read|admin"}` counter

**Fallback for Missing User ID**:
- If JWT is invalid or missing `sub` claim, fallback to IP-based limiting
- Log warning for investigation: `_logger.LogWarning("Rate limit applied to IP for authenticated request - missing user ID claim")`

**Decision**: Use ASP.NET Core 9.0 built-in rate limiting with partitioned keys. Read endpoints use IP, admin endpoints use JWT `sub` claim. Simple, performant, no external dependencies.

---

## Technology Stack Summary

All decisions align with Maliev microservices standards and CLAUDE.md requirements:

| Component | Technology | Version | Justification |
|-----------|-----------|---------|---------------|
| **Framework** | ASP.NET Core | 9.0 | Latest stable, microservices standard |
| **Database** | PostgreSQL | 18 | Constitutional requirement, proven performance |
| **ORM** | Entity Framework Core | 9.0.10 | Standard for .NET, mature tooling |
| **Caching** | StackExchange.Redis | 9.0.0 | Industry standard, high performance |
| **In-Memory Cache** | Microsoft.Extensions.Caching.Memory | 9.0.0 | Built-in, no SizeLimit (CRITICAL) |
| **Logging** | Serilog | 8.0.2 | Structured logging, stdout only |
| **Validation** | FluentValidation | 11.3.0 | Declarative, testable validation |
| **Resilience** | Polly | 8.5.0 | Circuit breaker, retry policies |
| **Metrics** | Prometheus.AspNetCore | 8.2.1 | Grafana integration, standard |
| **API Docs** | Scalar | 1.2.42 | Modern OpenAPI UI |
| **Authentication** | Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.8 | JWT standard |
| **Testing** | xUnit + FluentAssertions | Latest | Standard for .NET |

---

## Implementation Readiness

All research areas resolved with concrete implementation patterns. Ready to proceed to Phase 1 (Design Artifacts).

**Next Steps**:
1. Generate `data-model.md` with full entity schemas and migrations
2. Generate `contracts/openapi.yaml` with all 15 endpoints
3. Generate `quickstart.md` with local development setup
4. Re-evaluate Constitution Check with concrete technical decisions
