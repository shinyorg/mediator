namespace Shiny.Mediator.Infrastructure;

public record CacheItemConfig(
    TimeSpan? AbsoluteExpiration = null, 
    TimeSpan? SlidingExpiration = null
);

public record CacheEntry<T>(
    string Key,
    T Value,
    DateTimeOffset CreatedAt
);

public interface ICacheService
{
    /// <summary>
    /// This will retrieve the data from the factory if not present in cache and then add it to the cache once retrieved
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="config"></param>
    /// <typeparam name="T">Configuration of cache, assumes default if not set</typeparam>
    /// <returns></returns>
    Task<CacheEntry<T>?> GetOrCreate<T>(
        string key, 
        Func<Task<T>> factory,
        CacheItemConfig? config = null
    );
    
    /// <summary>
    /// Manually insert or overwrite an item in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="config">Configuration of cache, assumes default if not set</param>
    Task Set<T>(
        string key, 
        T value, 
        CacheItemConfig? config = null
    );


    /// <summary>
    /// Retrieves a cached value, null if not found
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<CacheEntry<T>?> Get<T>(string key);

    
    /// <summary>
    /// Clears cache keys starting with prefix
    /// </summary>
    /// <param name="requestKey"></param>
    /// <param name="partialMatch">If true, uses StartsWith on requestKey, otherwise looks for exact match</param>
    Task Remove(string requestKey, bool partialMatch = false);

    /// <summary>
    /// Clears the cache
    /// </summary>
    /// <returns></returns>
    Task Clear();
}