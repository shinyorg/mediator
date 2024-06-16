using System.Text.Json;

namespace Shiny.Mediator.Middleware;


public class CacheRequestMiddleware<TRequest, TResult>(
    IConnectivity connectivity, 
    IFileSystem fileSystem
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult> 
    // IRequestHandler<FlushAllCacheRequest>, 
    // IRequestHandler<FlushCacheItemRequest>
{ 
    readonly Dictionary<string, CachedItem<object>> memCache = new();
    
    
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler, 
        CancellationToken cancellationToken
    )
    {
        // no caching for void requests
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);

        var cfg = requestHandler.GetHandlerHandleMethodAttribute<TRequest, CacheAttribute>();
        if (cfg == null)
            return await next().ConfigureAwait(false);

        var result = await this.Process(cfg, request, next, requestHandler, cancellationToken).ConfigureAwait(false);
        return result;
    }


    public virtual async Task<TResult> Process(
        CacheAttribute cfg, 
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler, 
        CancellationToken cancellationToken
    )
    {
        var result = default(TResult);
        var connected = connectivity.NetworkAccess == NetworkAccess.Internet;
        if (cfg.OnlyForOffline && connected)
        {
            result = await this.GetAndStore(request, next, cfg).ConfigureAwait(false);
            return result;
        }
        
        var item = this.GetFromStore(request, cfg);
        if (item == null)
        {
            if (connected)
                result = await this.GetAndStore(request, next, cfg).ConfigureAwait(false);
        }
        else if (this.IsExpired(item, cfg))
        {
            result = item.Value;
            if (connected)
                result = await this.GetAndStore(request, next, cfg).ConfigureAwait(false);
        }
        else
            result = item.Value;
        
        return result;
    }
    

    protected virtual bool IsExpired(CachedItem<TResult> item, CacheAttribute cfg)
    {
        if (cfg.MaxAgeSeconds <= 0)
            return false;

        var expiry = item.CreatedOn.Add(TimeSpan.FromSeconds(cfg.MaxAgeSeconds));
        var expired = expiry < DateTimeOffset.UtcNow;
        return expired;
    }
    
    
    protected virtual async Task<TResult> GetAndStore(TRequest request, RequestHandlerDelegate<TResult> next, CacheAttribute cfg)
    {
        var result = await next().ConfigureAwait(false);
        this.Store(request, result, cfg);
        return result;
    }
    

    protected virtual string GetCacheKey(TRequest request)
    {
        if (request is ICacheItem item)
            return item.CacheKey;
        
        var key = $"{typeof(TRequest).Namespace}.{typeof(TRequest).Name}";
        return key;
    }
    
    
    protected virtual string GetCacheFilePath(TRequest request)
    {
        var key = this.GetCacheKey(request);
        var path = Path.Combine(fileSystem.CacheDirectory, $"{key}.cache");
        return path;
    }


    protected virtual void Store(TRequest request, TResult result, CacheAttribute cfg)
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


    protected virtual CachedItem<TResult>? GetFromStore(TRequest request, CacheAttribute cfg)
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

// public record FlushCacheItemRequest(Type RequestType) : IRequest;
// public record FlushAllCacheRequest : IRequest;

/// <summary>
/// Implementing this interface will allow you to create your own cache key, otherwise the cache key is based on the name
/// of the request model
/// </summary>
public interface ICacheItem
{
    string CacheKey { get; }
}

public record CachedItem<T>(
    DateTimeOffset CreatedOn,
    T Value
);

