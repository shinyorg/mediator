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
    public static CacheItemConfig DefaultCache = new CacheItemConfig
    {
        AbsoluteExpiration = TimeSpan.FromMinutes(10)
    };
    
    
    public async Task<CacheEntry<T>?> GetOrCreate<T>(string key, Func<Task<T>> retrieveFunc, CacheItemConfig? config = null)
    {
        var e = await this.TryGet<T>(key).ConfigureAwait(false);
        
        if (e == null)
        {
            var result = await retrieveFunc
                .Invoke()
                .ConfigureAwait(false);
            
            e = await this
                .Store(key, result, config)
                .ConfigureAwait(false);
        }
        return ToExternal(e);
    }
    
    
    public Task Set<T>(string key, T value, CacheItemConfig? config = null)
        => this.Store(key, value, config);

    
    public async Task<CacheEntry<T>?> Get<T>(string key)
    {
        var e = await this
            .TryGet<T>(key)
            .ConfigureAwait(false);

        return ToExternal(e);
    }
    

    public Task Remove(string requestKey, bool partialMatch = false)
        => storage.Remove(Category, requestKey, partialMatch);

    
    public Task Clear() => storage.Clear(Category);


    static CacheEntry<T>? ToExternal<T>(InternalCacheEntry<T>? e)
    {
        if (e == null)
            return null;

        return new(e.Key, e.Value, e.CreatedAt);
    }

    async Task<InternalCacheEntry<T>?> TryGet<T>(string key)
    {
        var e = await storage
            .Get<InternalCacheEntry<T>>(Category, key)
            .ConfigureAwait(false);
        
        if (e != null)
        {
            var now = timeProvider.GetUtcNow();
            
            if (e.ExpiresAt != null && e.ExpiresAt < now)
            {
                await storage.Remove(Category, e.Key).ConfigureAwait(false);
                e = null;
            }
            else if (e.Config?.SlidingExpiration != null)
            {
                var expiresAt = now.Add(e.Config.SlidingExpiration.Value);
                e = e with { ExpiresAt = expiresAt };
                await storage.Set(Category, e.Key, e).ConfigureAwait(false);
            }
        }

        return e;
    }
    
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
            config ?? DefaultCache
        );
        await storage.Set(Category, key, e).ConfigureAwait(false);
        
        return e;
    }
}