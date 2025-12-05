using Maliev.CountryService.Api.BackgroundServices;
using Maliev.CountryService.Api.HealthChecks;
using Maliev.CountryService.Api.Middleware;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("country-service"); // Register service meters for OpenTelemetry business metrics

builder.AddPostgresDbContext<CountryServiceDbContext>(connectionStringName: "CountryDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "MALIEV Country Service API";
            document.Info.Description = "Reference data service for country information. Provides lookup by ID, ISO 3166-1 alpha-2/alpha-3 codes with ETag-based caching, paginated listing with region/subregion filtering, name search, and administrative endpoints for bulk import and data updates.";
            document.Info.Version = "v1";
            return Task.CompletedTask;
        });
    });
}

builder.Services.AddControllers();

// Configure memory cache
builder.Services.AddMemoryCache();

// Configure Redis connection with StackExchange.Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    }
    catch (Exception)
    {
        // Don't register Redis - services will handle fallback
    }
}

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Read endpoints: 100 requests per minute per IP
    options.AddPolicy("read-endpoints", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Admin endpoints: 20 requests per minute per user (JWT sub claim)
    options.AddPolicy("admin-endpoints", context =>
    {
        var userId = context.User?.FindFirst("sub")?.Value ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: userId,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RATE_LIMIT_EXCEEDED",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = 60
        }, cancellationToken: token);
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CountryAdmin", policy =>
        policy.RequireRole("CountryAdmin", "SuperAdmin"));

    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireRole("SuperAdmin"));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database-connection", tags: new[] { "readiness" });

// Register application services for fast country lookup
builder.Services.AddSingleton<MemoryCacheService>();

// Register ICacheService - use Redis if available, otherwise MemoryCache
builder.Services.AddSingleton<ICacheService>(sp =>
{
    var redis = sp.GetService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
    var memoryCache = sp.GetRequiredService<MemoryCacheService>();

    return new RedisCacheService(logger, memoryCache, redis);
});

// Register business metrics
builder.Services.AddSingleton<Maliev.CountryService.Api.Metrics.BusinessMetrics>();

// Register degradation tracking
builder.Services.AddScoped<DegradationContext>();

// Register ICountryService
builder.Services.AddScoped<ICountryService, CountryService>();

// Register bulk import services
builder.Services.AddScoped<IBulkImportService, BulkImportService>();

// Register hosted services
builder.Services.AddHostedService<CacheWarmingService>();
builder.Services.AddHostedService<BulkImportWorkerService>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// --- Database Migrations ---
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.MigrateDatabaseAsync<CountryServiceDbContext>();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed - application may not function correctly");
        // Don't throw - allow app to start for debugging
    }
}

app.UseForwardedHeaders();

// Configure middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<DegradationHeaderMiddleware>();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors();

// JWT Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map health check and metrics endpoints via ServiceDefaults
app.MapDefaultEndpoints("countries");

app.MapControllers();

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "countries");

logger.LogInformation("CountryService started successfully");
await app.RunAsync();

/// <summary>
/// Entry point for the application.
/// </summary>
public partial class Program { }
