using System.Collections.Concurrent;
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

        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, CacheAttribute>();
        if (attribute == null)
            return await next().ConfigureAwait(false);
        
        var cacheKey = CacheExtensions.GetCacheKey(request!);
        var result = await cache.GetOrCreateAsync<TResult>(
            cacheKey,
            entry =>
            {
                this.SetCacheEntry(attribute, entry);
                return next();
            }
        );
        return result!;
    }

    
    protected void SetCacheEntry(CacheAttribute attribute, ICacheEntry entry)
    {
        entry.Priority = attribute.Priority;
        if (attribute.AbsoluteExpirationSeconds > 0)
            entry.AbsoluteExpirationRelativeToNow =
                TimeSpan.FromSeconds(attribute.AbsoluteExpirationSeconds);

        if (attribute.SlidingExpirationSeconds > 0)
            entry.SlidingExpiration = TimeSpan.FromSeconds(attribute.SlidingExpirationSeconds);
    }
}