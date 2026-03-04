using Maliev.CountryService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Maliev.CountryService.Infrastructure.Services;

/// <summary>
/// Redis distributed cache with stale-while-revalidate pattern.
/// Provides L2 caching with a 1-hour grace period for stale data, falling back to
/// <see cref="MemoryCacheService"/> when Redis is unavailable.
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

    // Stale-while-revalidate grace period
    private static readonly TimeSpan StaleGracePeriod = TimeSpan.FromHours(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fallbackCache">The fallback memory cache service.</param>
    /// <param name="redis">The Redis connection multiplexer (optional, may be null if Redis is unavailable).</param>
    public RedisCacheService(
        ILogger<RedisCacheService> logger,
        MemoryCacheService fallbackCache,
        IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
        _fallbackCache = fallbackCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        // Check L1 cache (Memory) FIRST for ultra-low latency
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
                return null;
            }

            var cacheEntry = JsonSerializer.Deserialize<CacheEntry<T>>((string)value!);
            if (cacheEntry == null || cacheEntry.Value == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var freshnessExpiry = cacheEntry.CachedAtUtc + cacheEntry.OriginalTtl;

            // Update L1 cache with what we found in L2
            await _fallbackCache.SetAsync(key, cacheEntry.Value, cacheEntry.OriginalTtl, cancellationToken);

            if (now > freshnessExpiry)
            {
                _logger.LogDebug("Serving stale cache entry for key {Key}, age {Age}s",
                    key, (now - cacheEntry.CachedAtUtc).TotalSeconds);
            }

            return cacheEntry.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET failed for key {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
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

            var cacheEntry = new CacheEntry<T>
            {
                Value = value,
                CachedAtUtc = DateTime.UtcNow,
                OriginalTtl = actualExpiration
            };

            var json = JsonSerializer.Serialize(cacheEntry);
            var extendedTtl = actualExpiration + StaleGracePeriod;
            await db.StringSetAsync(key, json, extendedTtl);

            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET failed for key {Key}, using fallback", key);
            await _fallbackCache.SetAsync(key, value, expiration, cancellationToken);
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
