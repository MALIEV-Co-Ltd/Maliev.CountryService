using Maliev.CountryService.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Maliev.CountryService.Api.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

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

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        _logger.LogDebug("Memory cache REMOVED key {Key}", key);
        await Task.CompletedTask;
    }

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