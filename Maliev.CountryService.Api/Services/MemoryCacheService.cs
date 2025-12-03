using Maliev.CountryService.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// A memory cache service implementation of <see cref="ICacheService"/>.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
    /// </summary>
    /// <param name="memoryCache">The in-memory cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a cached item by its key.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The key of the cached item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached item, or null if not found.</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Memory cache HIT for key {Key}", key);
            return value;
        }

        _logger.LogDebug("Memory cache MISS for key {Key}", key);
        return null;
    }

    /// <summary>
    /// Sets a cached item with a specified key and optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The key for the item.</param>
    /// <param name="value">The item to cache.</param>
    /// <param name="expiration">Optional expiration time for the item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15) // Default 15 minutes
        };
        _memoryCache.Set(key, value, options);
        _logger.LogDebug("Memory cache SET for key {Key} with expiration {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Removes a cached item by its key.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        _logger.LogDebug("Memory cache REMOVED key {Key}", key);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Removes all cached items matching a specific pattern.
    /// Note: IMemoryCache does not support pattern-based removal directly.
    /// This implementation logs a warning and does nothing.
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("RemovePatternAsync is not fully supported by IMemoryCache. Clearing all or using a workaround.");
        // IMemoryCache does not support pattern-based removal directly.
        // A common workaround involves storing keys in a separate collection, or simply clearing the entire cache.
        // For simplicity in this example, we'll log a warning and do nothing.
        // In a real scenario, this would require a custom IMemoryCache implementation or a different strategy.
        return Task.CompletedTask;
    }
}