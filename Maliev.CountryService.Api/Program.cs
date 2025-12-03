using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using HealthChecks.UI.Client;
using Maliev.CountryService.Api.BackgroundServices;
using Maliev.CountryService.Api.HealthChecks;
using Maliev.CountryService.Api.Middleware;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// T026: Add ServiceDefaults for OpenTelemetry, health checks, and service discovery
builder.AddServiceDefaults();

// Add custom business metrics to OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("country-service"));

try
{
    // T028: Google Secret Manager integration via ServiceDefaults
    builder.AddGoogleSecretManagerVolume();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // T033: Configure API versioning (v1 as default)
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // OpenAPI/Swagger for Scalar
    builder.Services.AddOpenApi();

    // T027: Configure DbContext with connection string from ServiceDefaults
    // Connection string name: "CountryDbContext" (not "CountryServiceDbContext")
    builder.AddPostgresDbContext<CountryServiceDbContext>("CountryDbContext");

    // T030: Configure memory cache (simple AddMemoryCache without SizeLimit - CRITICAL per CLAUDE.md)
    builder.Services.AddMemoryCache();

    // T031: Configure Redis connection with StackExchange.Redis
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

    // T043: Configure rate limiting (read endpoints: 100/min per IP, admin endpoints: 20/min per JWT sub claim)
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

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    "https://maliev.com",
                    "https://*.maliev.com",
                    "http://localhost:3000") // Allow local development
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("X-Correlation-ID", "ETag", "X-Cache", "X-Total-Count");
        });
    });

    // T042: Configure JWT authentication using ServiceDefaults (RSA PublicKey)
    // Reads from Jwt:PublicKey, Jwt:Issuer, Jwt:Audience
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        try
        {
            builder.AddJwtAuthentication();
        }
        catch (Exception)
        {
            // JWT configuration not found - API will start but authentication will not work
        }
    }

    // T044: Create authorization policies (CountryAdmin role, SuperAdmin role)
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

    // T029: Add health checks (database health check with "readiness" tag)
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database-connection", tags: new[] { "readiness" });

    // User Story 1: Register application services for fast country lookup
    // Register MemoryCacheService as fallback cache
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

    // User Story 6: Register degradation tracking
    builder.Services.AddScoped<DegradationContext>();

    // Register ICountryService
    builder.Services.AddScoped<ICountryService, CountryService>();

    // User Story 5: Register bulk import services
    builder.Services.AddScoped<IBulkImportService, BulkImportService>();

    // Register hosted services
    builder.Services.AddHostedService<CacheWarmingService>();
    builder.Services.AddHostedService<BulkImportWorkerService>();

    var app = builder.Build();

    app.UseForwardedHeaders();

    // T035: Configure middleware pipeline (exact order)
    // 1. Correlation ID (early in pipeline)
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 2. Security headers
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // 3. Exception handling
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 4. Degradation header (T122)
    app.UseMiddleware<DegradationHeaderMiddleware>();

    // T034: Configure Scalar UI (TODO: Fix Scalar API reference - using OpenAPI for now)
    app.MapOpenApi();

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseCors();

    // JWT Authentication & Authorization
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Map health check and metrics endpoints via ServiceDefaults
    app.MapDefaultEndpoints("countries");

    app.MapControllers();

    // Redirect root to OpenAPI documentation
    app.MapGet("/", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex}");
    throw;
}

// Make Program class accessible for integration tests
/// <summary>
/// Entry point for the application.
/// </summary>
public partial class Program { }
