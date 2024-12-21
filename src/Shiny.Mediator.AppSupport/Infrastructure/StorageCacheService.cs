using Shiny.Mediator.Caching;

namespace Shiny.Mediator.Infrastructure;


public class FileSystemCacheService(IStorageService storage) : ICacheService
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
        throw new NotImplementedException();
    }

    
    public void Remove(string key)
    {
        throw new NotImplementedException();
    }

    
    public void RemoveByPrefix(string prefix)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }
}