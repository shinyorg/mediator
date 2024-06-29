using Microsoft.Extensions.Caching.Memory;

namespace Shiny.Mediator.Caching.Infrastructure;


public class CachingRequestMiddleware<TRequest, TResult>(IMemoryCache cache) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);

        var cfg = requestHandler.GetHandlerHandleMethodAttribute<TRequest, CacheAttribute>();
        if (cfg == null)
            return await next().ConfigureAwait(false);
        
        var cacheKey = this.GetCacheKey(request!, requestHandler);
        var result = await cache.GetOrCreateAsync<TResult>(
            cacheKey,
            entry =>
            {
                entry.Priority = cfg.Priority;
        
                if (cfg.AbsoluteExpirationSeconds > 0)
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cfg.AbsoluteExpirationSeconds);

                if (cfg.SlidingExpirationSeconds > 0)
                    entry.SlidingExpiration = TimeSpan.FromSeconds(cfg.SlidingExpirationSeconds);
                
                return next();
            }
        );
        
        return result!;
    }
    

    protected virtual string GetCacheKey(object request, IRequestHandler handler)
    {
        if (request is IRequestKey keyProvider)
            return keyProvider.GetKey();
        
        var t = request.GetType();
        var key = $"{t.Namespace}_{t.Name}";
        return key;
    }
}