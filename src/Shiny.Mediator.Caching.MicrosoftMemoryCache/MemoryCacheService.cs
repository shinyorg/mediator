using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Caching;

namespace Shiny.Mediator;


public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> factory, CacheItemConfig? config = null)
        => cache.GetOrCreateAsync(
            key, 
            async e =>
            {
                var result = await factory().ConfigureAwait(false);
                return new CacheEntry<T>(
                    key,
                    result,
                    DateTimeOffset.UtcNow
                );
            }, 
            new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration,
                SlidingExpiration = config?.SlidingExpiration
            }
        );
    

    public Task Set<T>(string key, T value, CacheItemConfig? config = null)
    {
        // TODO: what if entry already exists?
        var entry = cache.CreateEntry(key);
        entry.Value = new CacheEntry<T>(key, value, DateTimeOffset.UtcNow);
        entry.AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration;
        entry.SlidingExpiration = config?.SlidingExpiration;
        
        return Task.CompletedTask;
    }

    public Task<CacheEntry<T>?> Get<T>(string key)
    {
        if (cache.TryGetValue(key, out var result) && result is CacheEntry<T> entry)
            return Task.FromResult(entry)!;

        return Task.FromResult<CacheEntry<T>?>(null);
    }


    public Task Remove(string key)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
    

    public Task RemoveByPrefix(string prefix)
    {
        var entries = cache.GetEntries();
        foreach (var entry in entries)
        {
            if (entry.Key is string key && key.StartsWith(prefix))
                cache.Remove(key); // TODO: altering enumerable
        }
        return Task.CompletedTask;
    }

    public Task Clear()
    {
        cache.Clear();
        return Task.CompletedTask;
    }
}