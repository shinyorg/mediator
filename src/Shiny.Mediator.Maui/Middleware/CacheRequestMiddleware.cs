using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator.Middleware;


public class CacheRequestMiddleware<TRequest, TResult>(
    IConfiguration configuration,
    IConnectivity connectivity, 
    IFileSystem fileSystem
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult> 
    // IRequestHandler<FlushAllCacheRequest>, 
    // IRequestHandler<FlushCacheItemRequest>
{ 
    public async Task<TResult> Process(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        // no caching for void requests
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);

        // no config no cache - TODO: could consider ICacheItem taking default values
        var cfg = GetConfiguration(request);
        if (cfg == null)
            return await next().ConfigureAwait(false);

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
    

    protected virtual bool IsExpired(CachedItem<TResult> item, CacheConfiguration cfg)
    {
        if (cfg.MaxAge == null)
            return false;

        var expiry = item.CreatedOn.Add(cfg.MaxAge.Value);
        var expired = expiry < DateTimeOffset.UtcNow;
        return expired;
    }
    
    
    protected virtual async Task<TResult> GetAndStore(TRequest request, RequestHandlerDelegate<TResult> next, CacheConfiguration cfg)
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


    protected virtual void Store(TRequest request, TResult result, CacheConfiguration cfg)
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


    protected virtual CachedItem<TResult>? GetFromStore(TRequest request, CacheConfiguration cfg)
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
    

    readonly Dictionary<string, CachedItem<object>> memCache = new();
    readonly object syncLock = new();
    CacheConfiguration[]? configurations;
    
    
    protected virtual CacheConfiguration? GetConfiguration(TRequest request)
    {
        var type = request.GetType();
        var key = $"{type.Namespace}.{type.Name}";

        if (this.configurations == null)
        {
            lock (this.syncLock)
            {
                this.configurations = configuration
                    .GetSection("Cache")
                    .Get<CacheConfiguration[]>();
            }
        }

        var cfg = this.configurations?
            .FirstOrDefault(x => x
                .RequestType
                .Equals(
                    key, 
                    StringComparison.InvariantCultureIgnoreCase
                )
            );
        
        return cfg;
    }
}

// public record FlushCacheItemRequest(Type RequestType) : IRequest;
// public record FlushAllCacheRequest : IRequest;

public interface ICacheItem
{
    string CacheKey { get; }
}

public record CachedItem<T>(
    DateTimeOffset CreatedOn,
    T Value
);

public enum StoreType
{
    File,
    Memory
}

public class CacheConfiguration
{
    public string RequestType { get; set; }
    public bool OnlyForOffline { get; set; }
    public TimeSpan? MaxAge { get; set; }
    public StoreType Storage { get; set; }
}