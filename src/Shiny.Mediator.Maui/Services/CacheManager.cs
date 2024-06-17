using System.Text.Json;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Maui.Services;


public class CacheManager(    
    IConnectivity connectivity, 
    IFileSystem fileSystem
)
{
    readonly Dictionary<string, CachedItem<object>> memCache = new();


    public void RemoveCacheItem(object request)
    {
        var cacheKey = this.GetCacheKey(request);
        lock (this.memCache)
            this.memCache.Remove(cacheKey);

        var path = this.GetCacheFilePath(request);
        if (File.Exists(path))
            File.Delete(path);
    }


    public void FlushAllCache()
    {
        lock (this.memCache)
            this.memCache.Clear();
        
        Directory
            .GetFiles(
                fileSystem.CacheDirectory, 
                "*.cache", 
                SearchOption.TopDirectoryOnly
            )
            .ToList()
            .ForEach(File.Delete);
    }
    
    public async Task<TResult> CacheOrGet<TResult>(
        CacheAttribute cfg,
        object request,
        Func<Task<TResult>> getFunc
    )
    {
        var result = default(TResult);
        var connected = connectivity.NetworkAccess == NetworkAccess.Internet;
        if (cfg.OnlyForOffline && connected)
        {
            result = await this.GetAndStore(request, getFunc, cfg).ConfigureAwait(false);
            return result;
        }
        
        var item = this.GetFromStore<TResult>(request, cfg);
        if (item == null)
        {
            if (connected)
                result = await this.GetAndStore(request, getFunc, cfg).ConfigureAwait(false);
        }
        else if (this.IsExpired(item, cfg))
        {
            result = item.Value;
            if (connected)
                result = await this.GetAndStore(request, getFunc, cfg).ConfigureAwait(false);
        }
        else
            result = item.Value;
        
        return result;
    }
    
    
    protected virtual bool IsExpired<TResult>(CachedItem<TResult> item, CacheAttribute cfg)
    {
        if (cfg.MaxAgeSeconds <= 0)
            return false;

        var expiry = item.CreatedOn.Add(TimeSpan.FromSeconds(cfg.MaxAgeSeconds));
        var expired = expiry < DateTimeOffset.UtcNow;
        return expired;
    }
    
    
    protected virtual async Task<TResult> GetAndStore<TResult>(object request, Func<Task<TResult>> getFunc, CacheAttribute cfg)
    {
        var result = await getFunc().ConfigureAwait(false);
        this.Store(request, result, cfg);
        return result;
    }
    

    protected virtual string GetCacheKey(object request)
    {
        if (request is ICacheItem item)
            return item.CacheKey;

        var t = request.GetType();
        var key = $"{t.Namespace}.{t.Name}";
        return key;
    }
    
    
    protected virtual string GetCacheFilePath(object request)
    {
        var key = this.GetCacheKey(request);
        var path = Path.Combine(fileSystem.CacheDirectory, $"{key}.cache");
        return path;
    }


    protected virtual void Store(object request, object result, CacheAttribute cfg)
    {
        switch (cfg.Storage)
        {
            case StoreType.File:
                var path = this.GetCacheFilePath(request);
                var json = JsonSerializer.Serialize(result);
                File.WriteAllText(path, json);
                break;
            
            case StoreType.Memory:
                var key = this.GetCacheKey(request);
                lock (this.memCache)
                {
                    this.memCache[key] = new CachedItem<object>(
                        DateTimeOffset.UtcNow,
                        result!
                    );
                }
                break;
        }
    }


    protected virtual CachedItem<TResult>? GetFromStore<TResult>(object request, CacheAttribute cfg)
    {
        CachedItem<TResult>? returnValue = null;
        
        switch (cfg.Storage)
        {
            case StoreType.File:
                var path = this.GetCacheFilePath(request);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var obj = JsonSerializer.Deserialize<TResult>(json)!;
                    var createdOn = File.GetCreationTimeUtc(path);
                    returnValue = new CachedItem<TResult>(createdOn, obj);
                }
                break;

            
            case StoreType.Memory:
                var key = this.GetCacheKey(request);
                lock (this.memCache)
                {
                    if (this.memCache.ContainsKey(key))
                    {
                        var item = this.memCache[key];
                        returnValue = new CachedItem<TResult>(item.CreatedOn, (TResult)item.Value);
                    }
                }
                break;
        }

        return returnValue;
    }    
}

public record CachedItem<T>(
    DateTimeOffset CreatedOn,
    T Value
);