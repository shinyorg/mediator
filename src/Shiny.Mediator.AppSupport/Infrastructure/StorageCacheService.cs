namespace Shiny.Mediator.Infrastructure;


record InternalCacheEntry<T>(
    string Key,
    T Value,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    CacheItemConfig? Config
);


public class StorageCacheService(
    IStorageService storage,
    TimeProvider timeProvider
) : ICacheService
{
    public const string Category = "Cache";
    
    
    public async Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> factory, CacheItemConfig? config = null)
    {
        var e = await storage
            .Get<InternalCacheEntry<T>>(Category, key)
            .ConfigureAwait(false);
        
        if (e != null)
        {
            if (e.ExpiresAt != null && e.ExpiresAt > DateTimeOffset.UtcNow)
            {
                await storage.Remove(Category, key).ConfigureAwait(false);
                e = null;
            }
            else if (e.Config?.SlidingExpiration != null)
            {
                var now = timeProvider.GetUtcNow();
                var expiresAt = now.Add(e.Config.SlidingExpiration.Value);
                e = e with { ExpiresAt = expiresAt };
                await storage.Set(Category, key, e).ConfigureAwait(false);
            }
        }
        
        if (e == null)
        {
            var result = await factory.Invoke().ConfigureAwait(false);
            e = await this.Store(key, result, config).ConfigureAwait(false);
        }

        return new CacheEntry<T>(e.Key, e.Value, e.CreatedAt);
    }
    
    
    public Task Set<T>(string key, T value, CacheItemConfig? config = null)
        => this.Store(key, value, config);

    
    public async Task<CacheEntry<T>?> Get<T>(string key)
    {
        var entry = await storage.Get<InternalCacheEntry<T>>(Category, key).ConfigureAwait(false);
        if (entry == null)
            return null;
        
        return new CacheEntry<T>(key, entry.Value, entry.CreatedAt);
    }


    public Task Remove(string requestKey, bool partialMatch = false)
        => storage.Remove(Category, requestKey, partialMatch);

    
    public Task Clear() => storage.Clear(Category);

    async Task<InternalCacheEntry<T>> Store<T>(string key, T result, CacheItemConfig? config)
    {
        DateTimeOffset? expiresAt = null;
        var now = timeProvider.GetUtcNow();
        
        if (config != null)
        {
            if (config.AbsoluteExpiration != null)
            {
                expiresAt = now.Add(config.AbsoluteExpiration.Value);
            }   
            else if (config.SlidingExpiration != null)
            {
                expiresAt = now.Add(config.SlidingExpiration.Value);
            }
        }
        var e = new InternalCacheEntry<T>(
            key,
            result,
            now,
            expiresAt,
            config
        );
        await storage.Set(Category, key, e).ConfigureAwait(false);
        
        return e;
    }
}