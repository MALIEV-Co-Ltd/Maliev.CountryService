using Maliev.CountryService.Api.BackgroundServices;

using Maliev.CountryService.Api.Middleware;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddStandardMiddleware(options =>
{
    options.EnableRequestLogging = true;
});
builder.AddServiceMeters("countries-meter"); // Register service meters for OpenTelemetry business metrics

// Register DbContext for all environments (test factory provides connection string via environment variables)
builder.AddPostgresDbContext<CountryDbContext>(connectionName: "CountryDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// --- Authorization & Permissions ---
builder.Services.AddPermissionAuthorization();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.AddStandardOpenApi(
        title: "MALIEV Country Service API",
        description: "Reference data service for country information. Provides lookup by ID, ISO 3166-1 alpha-2/alpha-3 codes with ETag-based caching, paginated listing with region/subregion filtering, name search, and administrative endpoints for bulk import and data updates.");
}

builder.Services.AddControllers();

// Configure memory cache
builder.Services.AddMemoryCache();

// Redis Distributed Cache (ServiceDefaults)
builder.AddRedisDistributedCache(instanceName: "country:");

// MassTransit with RabbitMQ
builder.AddMassTransitWithRabbitMq();

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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register application services for fast country lookup
builder.Services.AddSingleton<MemoryCacheService>();

// Register ICacheService - RedisCacheService handles IDistributedCache injection
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

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
builder.AddIAMServiceClient("country");
builder.Services.AddIAMRegistration<CountryIAMRegistrationService>("country");

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// --- Database Migrations ---
await app.MigrateDatabaseAsync<CountryDbContext>();

app.UseForwardedHeaders();

// Configure middleware pipeline
app.UseMiddleware<DegradationHeaderMiddleware>();
app.UseStandardMiddleware();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseCors();

// JWT Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Log permission denials
app.UseMiddleware<PermissionDenialLoggingMiddleware>();

// Map health check and metrics endpoints via ServiceDefaults
app.MapDefaultEndpoints("country");

app.MapControllers();

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "country");

logger.LogInformation("CountryService started successfully");
await app.RunAsync();

/// <summary>
/// Entry point for the application.
/// </summary>
public partial class Program { }
