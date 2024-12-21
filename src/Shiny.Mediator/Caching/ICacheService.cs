namespace Shiny.Mediator.Caching;

public record CacheItemConfig(
    TimeSpan? AbsoluteExpiration = null, 
    TimeSpan? SlidingExpiration = null
);

public record CacheEntry<T>(
    string Key,
    T Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt
);

public interface ICacheService
{
    /// <summary>
    /// This will retrieve the data from the factory if not present in cache and then add it to the cache once retrieved
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="config"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<CacheEntry<T>> GetOrCreate<T>(
        string key, 
        Func<Task<T>> factory,
        CacheItemConfig? config = null
    );
    
    /// <summary>
    /// Get an item from cache if present - otherwise return null
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    CacheEntry<T>? Get<T>(string key);
    
    /// <summary>
    /// Manually insert or overwrite an item in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="config"></param>
    void Set(
        string key, 
        object value, 
        CacheItemConfig? config = null
    );

    /// <summary>
    /// Removes a specific cache item
    /// </summary>
    /// <param name="key"></param>
    void Remove(string key);
    
    
    /// <summary>
    /// Clears cache keys starting with prefix
    /// </summary>
    /// <param name="prefix"></param>
    void RemoveByPrefix(string prefix);

    /// <summary>
    /// Clears all cache
    /// </summary>
    void Clear();
}