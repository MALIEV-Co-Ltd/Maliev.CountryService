using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Maliev.CountryService.Api.HealthChecks;

/// <summary>
/// T125: Redis health check with circuit breaker state awareness.
/// Returns Degraded when Redis is unavailable but service continues with in-memory fallback.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(ILogger<RedisHealthCheck> logger, IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_redis == null || !_redis.IsConnected)
            {
                // T125: Circuit breaker state tracking
                _logger.LogWarning("Redis connection is not available - circuit may be open, using in-memory fallback");
                return HealthCheckResult.Degraded(
                    "Redis is unavailable but service continues with in-memory cache fallback",
                    data: new Dictionary<string, object>
                    {
                        { "circuit_breaker_state", "open_or_unavailable" },
                        { "fallback_active", true }
                    });
            }

            var database = _redis.GetDatabase();
            var pingResult = await database.PingAsync();

            if (pingResult.TotalMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Redis ping latency is high: {pingResult.TotalMilliseconds}ms",
                    data: new Dictionary<string, object>
                    {
                        { "latency_ms", pingResult.TotalMilliseconds },
                        { "circuit_breaker_state", "closed" }
                    });
            }

            return HealthCheckResult.Healthy(
                $"Redis is healthy (ping: {pingResult.TotalMilliseconds}ms)",
                data: new Dictionary<string, object>
                {
                    { "latency_ms", pingResult.TotalMilliseconds },
                    { "circuit_breaker_state", "closed" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed - circuit may be opening");
            return HealthCheckResult.Degraded(
                "Redis health check failed but service continues with fallback",
                ex,
                data: new Dictionary<string, object>
                {
                    { "circuit_breaker_state", "potentially_opening" },
                    { "fallback_active", true }
                });
        }
    }
}

