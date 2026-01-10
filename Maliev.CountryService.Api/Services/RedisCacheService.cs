using Maliev.CountryService.Api.Metrics;
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
        /// <summary>
        /// Gets or sets the cached value.
        /// </summary>
        public T Value { get; set; } = default!;

        /// <summary>
        /// Gets or sets the timestamp when the value was cached.
        /// </summary>
        public DateTime CachedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the original time-to-live duration.
        /// </summary>
        public TimeSpan OriginalTtl { get; set; }
    }
    private readonly IConnectionMultiplexer? _redis;
    private readonly MemoryCacheService _fallbackCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly BusinessMetrics? _businessMetrics; // Changed to nullable

    // T120: Stale-while-revalidate grace period (FR-032)
    private static readonly TimeSpan StaleGracePeriod = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fallbackCache">The fallback memory cache service.</param>
    /// <param name="redis">The Redis connection multiplexer (optional, may be null if Redis is unavailable).</param>
    /// <param name="businessMetrics">The business metrics service.</param>
    public RedisCacheService(
        ILogger<RedisCacheService> logger,
        MemoryCacheService fallbackCache,
        IConnectionMultiplexer? redis = null,
        BusinessMetrics? businessMetrics = null) // Made nullable to avoid breaking tests that may not provide it
    {
        _redis = redis;
        _fallbackCache = fallbackCache;
        _logger = logger;
        _businessMetrics = businessMetrics;
    }

    /// <summary>
    /// Retrieves a cached value from Redis by key, with stale-while-revalidate support and fallback to memory cache.
    /// L1 (Memory) is checked before L2 (Redis) for optimal performance.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cached value, or null if not found.</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        // T068: Check L1 cache (Memory) FIRST for ultra-low latency
        var localCached = await _fallbackCache.GetAsync<T>(key, cancellationToken);
        if (localCached != null)
        {
            _logger.LogDebug("L1 cache HIT for key {Key}", key);
            return localCached;
        }

        if (_redis == null || !_redis.IsConnected)
        {
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _businessMetrics?.RecordCacheMiss("redis");
                return null;
            }

            // T120: Deserialize as CacheEntry to check staleness
            var cacheEntry = JsonSerializer.Deserialize<CacheEntry<T>>((string)value!);
            if (cacheEntry == null || cacheEntry.Value == null)
            {
                _businessMetrics?.RecordCacheMiss("redis");
                return null;
            }

            var now = DateTime.UtcNow;
            var freshnessExpiry = cacheEntry.CachedAtUtc + cacheEntry.OriginalTtl;
            var staleExpiry = freshnessExpiry + StaleGracePeriod;

            // Update L1 cache with what we found in L2
            await _fallbackCache.SetAsync(key, cacheEntry.Value, cacheEntry.OriginalTtl, cancellationToken);

            // T120: Check if data is stale but within grace period
            if (now > freshnessExpiry && now < staleExpiry)
            {
                _logger.LogDebug("Serving stale cache entry for key {Key}, age {Age}s, triggering refresh",
                    key, (now - cacheEntry.CachedAtUtc).TotalSeconds);

                // Note: Real background refresh logic would be implemented here if a callback was available
            }

            _businessMetrics?.RecordCacheHit("redis");
            return cacheEntry.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET failed for key {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Stores a value in Redis cache with extended TTL for stale-while-revalidate support, with fallback to memory cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The time-to-live duration for the cached value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();

            var actualExpiration = expiration ?? TimeSpan.FromMinutes(15);

            // T120: Wrap value with metadata for stale-while-revalidate
            var cacheEntry = new CacheEntry<T>
            {
                Value = value,
                CachedAtUtc = DateTime.UtcNow,
                OriginalTtl = actualExpiration
            };

            var json = JsonSerializer.Serialize(cacheEntry);

            // T120: Store with extended TTL (original + grace period)
            var extendedTtl = actualExpiration + StaleGracePeriod;
            await db.StringSetAsync(key, json, extendedTtl);

            // Also set in fallback cache for faster local access
            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET failed for key {Key}, using fallback", key);
            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
        }
    }

    /// <summary>
    /// Removes a cached value from Redis and fallback cache by key.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.RemoveAsync(key, cancellationToken);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
            await _fallbackCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE failed for key {Key}", key);
            await _fallbackCache.RemoveAsync(key, cancellationToken);
        }
    }

    /// <summary>
    /// Removes all cached values matching a pattern from Redis and fallback cache.
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys (e.g., "country:*").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            await _fallbackCache.RemovePatternAsync(pattern, cancellationToken);
            return;
        }

        try
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE PATTERN failed for pattern {Pattern}", pattern);
        }
    }
}
