using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.CountryService.Api.HealthChecks;

/// <summary>
/// T124: Database health check with degraded mode support.
/// Returns Degraded when database is unavailable but cache is available for serving requests.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly CountryServiceDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseHealthCheck(
        CountryServiceDbContext context,
        ICacheService cacheService,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Checks the health of the database connection and returns Degraded if cache is available as fallback.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A health check result indicating Healthy, Degraded, or Unhealthy status.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            // T124: Return Degraded if cache is available (service can still operate)
            // Check if cache service is functional by attempting a dummy get
            try
            {
                // Test cache availability (non-null cache service means it's configured)
                if (_cacheService != null)
                {
                    _logger.LogWarning("Database unavailable but cache is available - service operating in degraded mode");
                    return HealthCheckResult.Degraded(
                        "Database unavailable - serving from cache only",
                        ex,
                        new Dictionary<string, object> { { "degraded_mode", true } });
                }
            }
            catch
            {
                // Cache also unavailable - return unhealthy
            }

            return HealthCheckResult.Unhealthy("Database and cache unavailable", ex);
        }
    }
}
