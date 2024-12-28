using Shiny.Mediator.Caching;

namespace Shiny.Mediator.Infrastructure;


record InternalCacheEntry<T>(
    string Key,
    T Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    CacheItemConfig? Config
);


public class StorageCacheService(IStorageService storage) : ICacheService
{
    // TODO: check expiry or clear it out?
    public async Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> factory, CacheItemConfig? config = null)
    {
        var e = await storage.Get<InternalCacheEntry<T>>(key).ConfigureAwait(false);
        if (e != null)
        {
            if (e.ExpiresAt != null && e.ExpiresAt > DateTimeOffset.UtcNow)
            {
                await storage.Remove(key).ConfigureAwait(false);
                e = null;
            }
            else if (e.Config?.SlidingExpiration != null)
            {
                var expiresAt = DateTimeOffset.UtcNow.Add(e.Config.SlidingExpiration.Value);
                e = e with { ExpiresAt = expiresAt };
                await storage.Set(key, e).ConfigureAwait(false);
            }
        }
        
        if (e == null)
        {
            var result = await factory.Invoke().ConfigureAwait(false);
            await this.Store(key, result, config).ConfigureAwait(false);
        }

        return null;
    }
    
    
    public Task Set<T>(string key, T value, CacheItemConfig? config = null)
        => this.Store(key, value, config);

    public async Task<CacheEntry<T>?> Get<T>(string key)
    {
        var entry = await storage.Get<InternalCacheEntry<T>>(key).ConfigureAwait(false);
        if (entry == null)
            return null;
        
        return new CacheEntry<T>(key, entry.Value, entry.CreatedAt);
    }


    public Task Remove(string key) => storage.Remove(key);
    public Task RemoveByPrefix(string prefix)  => storage.RemoveByPrefix(prefix);
    public Task Clear() => storage.Clear();

    async Task<InternalCacheEntry<T>> Store<T>(string key, T result, CacheItemConfig? config)
    {
        DateTimeOffset? expiresAt = null;
            
        if (config != null)
        {
            if (config.AbsoluteExpiration != null)
            {
                expiresAt = DateTimeOffset.UtcNow.Add(config.AbsoluteExpiration.Value);
            }   
            else if (config.SlidingExpiration != null)
            {
                expiresAt = DateTimeOffset.UtcNow.Add(config.SlidingExpiration.Value);
            }
        }
        var e = new InternalCacheEntry<T>(
            key,
            result,
            DateTimeOffset.UtcNow,
            expiresAt,
            config
        );
        await storage.Set(key, e).ConfigureAwait(false);
        
        return e;
    }
}