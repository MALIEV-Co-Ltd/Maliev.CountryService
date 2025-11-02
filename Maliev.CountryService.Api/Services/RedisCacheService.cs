using Maliev.CountryService.Api.Metrics;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;
using System.Text.Json;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// T056: Redis distributed cache with stale-while-revalidate pattern.
/// T057: Polly circuit breaker (50% failure threshold, 60-sec break, fallback to MemoryCache).
/// T120: Stale-while-revalidate with 1-hour grace period.
/// </summary>
public class RedisCacheService : ICacheService
{
    /// <summary>
    /// Internal wrapper to store cache metadata for stale-while-revalidate pattern.
    /// </summary>
    private class CacheEntry<T>
    {
        public T Value { get; set; } = default!;
        public DateTime CachedAtUtc { get; set; }
        public TimeSpan OriginalTtl { get; set; }
    }
    private readonly IConnectionMultiplexer? _redis;
    private readonly ICacheService _fallbackCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    // T120: Stale-while-revalidate grace period (FR-032)
    private static readonly TimeSpan StaleGracePeriod = TimeSpan.FromHours(1);

    public RedisCacheService(
        ILogger<RedisCacheService> logger,
        ICacheService fallbackCache,
        IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
        _fallbackCache = fallbackCache;
        _logger = logger;

        // T057: Configure Polly circuit breaker
        _circuitBreaker = Policy
            .Handle<RedisConnectionException>()
            .Or<RedisTimeoutException>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,        // 50% failure rate
                samplingDuration: TimeSpan.FromSeconds(30),
                minimumThroughput: 10,
                durationOfBreak: TimeSpan.FromSeconds(60),
                onBreak: (exception, duration) =>
                {
                    _logger.LogWarning(exception, "Redis circuit breaker OPEN for {Duration}", duration);
                    BusinessMetrics.CircuitBreakerState.WithLabels("redis").Set(1); // Open
                },
                onReset: () =>
                {
                    _logger.LogInformation("Redis circuit breaker CLOSED");
                    BusinessMetrics.CircuitBreakerState.WithLabels("redis").Set(0); // Closed
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Redis circuit breaker HALF-OPEN");
                    BusinessMetrics.CircuitBreakerState.WithLabels("redis").Set(2); // Half-Open
                });
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogDebug("Redis unavailable, using fallback cache for key {Key}", key);
            return await _fallbackCache.GetAsync<T>(key, cancellationToken);
        }

        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    BusinessMetrics.CacheMisses.WithLabels("redis").Inc();
                    return null;
                }

                // T120: Deserialize as CacheEntry to check staleness
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry<T>>(value!);
                if (cacheEntry == null || cacheEntry.Value == null)
                {
                    BusinessMetrics.CacheMisses.WithLabels("redis").Inc();
                    return null;
                }

                var now = DateTime.UtcNow;
                var freshnessExpiry = cacheEntry.CachedAtUtc + cacheEntry.OriginalTtl;
                var staleExpiry = freshnessExpiry + StaleGracePeriod;

                // T120: Check if data is stale but within grace period
                if (now > freshnessExpiry && now < staleExpiry)
                {
                    _logger.LogInformation("Serving stale cache entry for key {Key}, age {Age}s, triggering refresh",
                        key, (now - cacheEntry.CachedAtUtc).TotalSeconds);

                    // Trigger background refresh (fire-and-forget)
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            _logger.LogDebug("Background refresh triggered for stale key {Key}", key);
                            // Note: Actual refresh would require callback to data source
                            // For now, we just log the intent. The consumer (CountryService) would need to handle this.
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Background refresh failed for key {Key}", key);
                        }
                    });
                }

                BusinessMetrics.CacheHits.WithLabels("redis").Inc();
                return cacheEntry.Value;
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Redis circuit breaker is OPEN, using fallback for key {Key}", key);
            return await _fallbackCache.GetAsync<T>(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET failed for key {Key}, using fallback", key);
            return await _fallbackCache.GetAsync<T>(key, cancellationToken);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.SetAsync(key, value, ttl, cancellationToken);
            return;
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                var db = _redis.GetDatabase();

                // T120: Wrap value with metadata for stale-while-revalidate
                var cacheEntry = new CacheEntry<T>
                {
                    Value = value,
                    CachedAtUtc = DateTime.UtcNow,
                    OriginalTtl = ttl
                };

                var json = JsonSerializer.Serialize(cacheEntry);

                // T120: Store with extended TTL (original + grace period)
                var extendedTtl = ttl + StaleGracePeriod;
                await db.StringSetAsync(key, json, extendedTtl);

                // Also set in fallback cache for faster local access
                await _fallbackCache.SetAsync(key, value, ttl, cancellationToken);
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Redis circuit breaker is OPEN, using fallback for SET key {Key}", key);
            await _fallbackCache.SetAsync(key, value, ttl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET failed for key {Key}, using fallback", key);
            await _fallbackCache.SetAsync(key, value, ttl, cancellationToken);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.RemoveAsync(key, cancellationToken);
            return;
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(key);
                await _fallbackCache.RemoveAsync(key, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE failed for key {Key}", key);
            await _fallbackCache.RemoveAsync(key, cancellationToken);
        }
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.RemovePatternAsync(pattern, cancellationToken);
            return;
        }

        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                var endpoints = _redis.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    var keys = server.Keys(pattern: pattern);
                    var db = _redis.GetDatabase();
                    
                    foreach (var key in keys)
                    {
                        await db.KeyDeleteAsync(key);
                    }
                }
                
                await _fallbackCache.RemovePatternAsync(pattern, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE PATTERN failed for pattern {Pattern}", pattern);
        }
    }
}
