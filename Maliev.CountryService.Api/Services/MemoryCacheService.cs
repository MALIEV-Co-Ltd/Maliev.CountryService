using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// In-memory cache service using IMemoryCache as fallback when Redis is unavailable.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Memory cache HIT for key {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Memory cache MISS for key {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        _cache.Set(key, value, options);
        _logger.LogDebug("Memory cache SET for key {Key} with TTL {Ttl}", key, ttl);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _logger.LogDebug("Memory cache REMOVE for key {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // MemoryCache doesn't support pattern matching - would need to track keys separately
        _logger.LogWarning("Pattern removal not supported in MemoryCache: {Pattern}", pattern);
        return Task.CompletedTask;
    }
}
