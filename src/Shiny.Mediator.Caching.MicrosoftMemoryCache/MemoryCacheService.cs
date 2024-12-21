using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Caching;

namespace Shiny.Mediator;


public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<CacheEntry<T>> GetOrCreate<T>(string key, Func<Task<T>> factory, CacheItemConfig? config = null)
    {
        throw new NotImplementedException();
    }
    

    public CacheEntry<T>? Get<T>(string key)
    {
        throw new NotImplementedException();
    }

    public void Set(string key, object value, CacheItemConfig? config = null)
    {
        var entry = cache.CreateEntry(key);
        // entry.Value = new CacheEntry<>(value); // TODO: hmmmm
        entry.AbsoluteExpirationRelativeToNow = config?.AbsoluteExpiration;
        entry.SlidingExpiration = config?.SlidingExpiration;
    }

    public void Remove(string key) => cache.Remove(key);
    public void RemoveByPrefix(string prefix)
    {
        var entries = cache.GetEntries();
        foreach (var entry in entries)
        {
            if (entry.Key is string key && key.StartsWith(prefix))
                cache.Remove(key); // TODO: altering enumerable
        }
    }
    public void Clear() => cache.Clear();
}