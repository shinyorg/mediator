namespace Shiny.Mediator.Infrastructure;

/// <summary>
/// Configuration for a single cache entry's expiration policy.
/// </summary>
/// <param name="AbsoluteExpiration">Absolute time-to-live measured from creation. Null disables absolute expiration.</param>
/// <param name="SlidingExpiration">Sliding time-to-live reset on each access. Null disables sliding expiration.</param>
public record CacheItemConfig(
    TimeSpan? AbsoluteExpiration = null,
    TimeSpan? SlidingExpiration = null
);

/// <summary>
/// A typed cache entry returned by <see cref="ICacheService"/>.
/// </summary>
/// <param name="Key">The full cache key.</param>
/// <param name="Value">The cached value.</param>
/// <param name="CreatedAt">UTC time the entry was first created.</param>
public record CacheEntry<T>(
    string Key,
    T Value,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Abstraction over the cache backend used by mediator caching middleware. Implementations may use in-memory
/// storage, distributed caches, or persistent stores.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached entry for <paramref name="key"/>, or invokes <paramref name="factory"/> and stores its result.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Producer invoked on a cache miss.</param>
    /// <param name="config">Optional expiration configuration; defaults are used when null.</param>
    /// <param name="cancellationToken"></param>
    Task<CacheEntry<T>?> GetOrCreate<T>(
        string key,
        Func<Task<T>> factory,
        CacheItemConfig? config = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Inserts or overwrites the value at <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="config">Optional expiration configuration; defaults are used when null.</param>
    /// <param name="cancellationToken"></param>
    Task<CacheEntry<T>> Set<T>(
        string key,
        T value,
        CacheItemConfig? config = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the cached entry for <paramref name="key"/>, or <c>null</c> when absent.
    /// </summary>
    Task<CacheEntry<T>?> Get<T>(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Removes entries matching <paramref name="requestKey"/>.
    /// </summary>
    /// <param name="requestKey">The exact key or key prefix to remove.</param>
    /// <param name="partialMatch">When true, treat <paramref name="requestKey"/> as a prefix (StartsWith) and remove all matches.</param>
    /// <param name="cancellationToken"></param>
    Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every entry from the cache.
    /// </summary>
    Task Clear(CancellationToken cancellationToken);
}
