using Maliev.CountryService.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Maliev.CountryService.Infrastructure.Services;

/// <summary>
/// A memory cache service implementation of <see cref="ICacheService"/>.
/// Provides L1 (in-process) caching as a fast local store and fallback for Redis.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

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

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Memory cache HIT for key {Key}", key);
            return value;
        }

        _logger.LogDebug("Memory cache MISS for key {Key}", key);
        return await Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15),
            Size = 1
        };
        options.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            _keys.TryRemove(k.ToString()!, out _);
        });

        _memoryCache.Set(key, value, options);
        _keys.TryAdd(key, 0);
        _logger.LogDebug("Memory cache SET for key {Key} with expiration {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        _keys.TryRemove(key, out _);
        _logger.LogDebug("Memory cache REMOVED key {Key}", key);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing pattern {Pattern} from memory cache", pattern);

        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        var keysToRemove = _keys.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
