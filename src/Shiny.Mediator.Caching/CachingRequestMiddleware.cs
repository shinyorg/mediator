﻿using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching;


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
        var e = cache.CreateEntry(cacheKey);
        e.Priority = cfg.Priority;
        
        if (cfg.AbsoluteExpirationSeconds != null)
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cfg.AbsoluteExpirationSeconds.Value);

        if (cfg.SlidingExpirationSeconds != null)
            e.SlidingExpiration = TimeSpan.FromSeconds(cfg.SlidingExpirationSeconds.Value);
        
        var result = await cache.GetOrCreateAsync<TResult>(
            e,
            _ => next()
        );
        return result!;
    }


    protected virtual string GetCacheKey(object request, IRequestHandler handler)
        => Utils.GetRequestKey(request);
}