namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Represents a service for caching data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached item by its key.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The key of the cached item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached item, or null if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    /// <summary>
    /// Sets a cached item with a specified key and optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The key for the item.</param>
    /// <param name="value">The item to cache.</param>
    /// <param name="expiration">Optional expiration time for the item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    /// <summary>
    /// Removes a cached item by its key.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes all cached items matching a specific pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);
}
