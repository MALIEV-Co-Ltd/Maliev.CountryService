using Maliev.Aspire.ServiceDefaults;
using Maliev.CountryService.Api.BackgroundServices;
using Maliev.CountryService.Api.Middleware;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Infrastructure.Data;
using Maliev.CountryService.Infrastructure.Data.SeedData;
using Maliev.CountryService.Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;
// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Program.Log.StartingHost(bootstrapLogger, "Country Service");

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
    builder.AddStandardCors(); // CORS with fail-fast validation
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

    // Add Response Caching
    builder.Services.AddResponseCaching();

    // Configure memory cache
    builder.Services.AddMemoryCache();

    // Redis Distributed Cache (ServiceDefaults)
    builder.AddStandardCache("country:"); // Redis + in-memory fallback, memory-optimized

    // MassTransit with RabbitMQ
    builder.AddMassTransitWithRabbitMq();

    // Configure rate limiting
    builder.AddStandardRateLimiting(); // Memory-optimized for low-spec nodes

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Register ICountryDbContext so middleware/services can inject it by interface
    builder.Services.AddScoped<ICountryDbContext>(sp => sp.GetRequiredService<CountryDbContext>());

    // Register repositories
    builder.Services.AddScoped<ICountryRepository, Maliev.CountryService.Infrastructure.Data.Repositories.CountryRepository>();
    builder.Services.AddScoped<IBulkImportJobRepository, Maliev.CountryService.Infrastructure.Data.Repositories.BulkImportJobRepository>();

    // Register application services for fast country lookup
    builder.Services.AddSingleton<MemoryCacheService>();

    // Register ICacheService - RedisCacheService handles IDistributedCache injection
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    // Register business metrics
    builder.Services.AddSingleton<Maliev.CountryService.Api.Metrics.BusinessMetrics>();

    // Register degradation tracking
    builder.Services.AddScoped<IDegradationContext, DegradationContext>();

    // Register ICountryService
    builder.Services.AddScoped<ICountryService, Maliev.CountryService.Application.Services.CountryService>();

    // Register bulk import services
    builder.Services.AddScoped<IBulkImportService, Maliev.CountryService.Application.Services.BulkImportService>();

    // Register hosted services
    builder.Services.AddHostedService<CacheWarmingService>();
    builder.Services.AddHostedService<BulkImportWorkerService>();
    builder.AddIAMServiceClient("country");
    builder.Services.AddIAMRegistration<Maliev.CountryService.Api.Services.CountryIAMRegistrationService>("country");

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<CountryDbContext>();
    await app.SeedCountriesAsync();

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
    app.UseResponseCaching();

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

    Program.Log.ServiceStarted(logger, "Country Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Program.Log.HostTerminated(bootstrapLogger, ex, "Country Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Entry point for the application.
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);
    }
}
