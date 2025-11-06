using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
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
using Prometheus;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// T026: Configure Serilog (console only, structured JSON logging)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Maliev Country Service");

    // T028: Google Secret Manager integration (/mnt/secrets path, optional for development)
    var secretsPath = "/mnt/secrets";
    if (Directory.Exists(secretsPath))
    {
        builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
        Log.Information("Loaded secrets from {SecretPath}", secretsPath);
    }

    // Add services to the container
    builder.Services.AddControllers();

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

    // T027: Configure DbContext with connection string from configuration, retry on failure
    builder.Services.AddDbContext<CountryServiceDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("CountryServiceDbContext");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    });

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
            Log.Information("Redis connection established: {RedisEndpoint}", redisConnectionString);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Redis connection failed - will use in-memory cache fallback");
            // Don't register Redis - services will handle fallback
        }
    }
    else
    {
        Log.Warning("Redis connection string not configured - using in-memory cache only");
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

    // T042: Configure JWT authentication (JwtBearer with validation)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        var jwtIssuer = builder.Configuration["JwtBearer:Issuer"];
        var jwtAudience = builder.Configuration["JwtBearer:Audience"];
        var jwtSecurityKey = builder.Configuration["JwtBearer:SecurityKey"];

        if (!string.IsNullOrEmpty(jwtIssuer) && !string.IsNullOrEmpty(jwtSecurityKey))
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecurityKey))
                };
            });

            Log.Information("JWT authentication configured for issuer: {Issuer}", jwtIssuer);
        }
        else
        {
            Log.Warning("JWT configuration not found - API will start but authentication will not work");
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
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // T029: Add health checks (database EF Core check with "readiness" tag, Redis health check)
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddDbContextCheck<CountryServiceDbContext>("database", tags: new[] { "readiness" })
        .AddCheck<DatabaseHealthCheck>("database-connection", tags: new[] { "readiness" });

    // Add Redis health check if Redis is configured
    if (builder.Services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
    {
        healthChecksBuilder.AddCheck<RedisHealthCheck>("redis", tags: new[] { "readiness" });
    }

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

    // T032: Configure Prometheus metrics (UseHttpMetrics middleware)
    app.UseHttpMetrics();

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseCors();

    // JWT Authentication & Authorization
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Health check endpoints (allow anonymous access for monitoring)
    app.MapGet("/countries/v1/liveness", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
        .AllowAnonymous()
        .WithTags("Health");

    app.MapHealthChecks("/countries/v1/readiness", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Metrics endpoint
    app.MapMetrics("/metrics").AllowAnonymous();

    app.MapControllers();

    Log.Information("Maliev Country Service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
