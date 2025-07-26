﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


public class CachingRequestMiddleware<TRequest, TResult>(
    ILogger<CachingRequestMiddleware<TRequest, TResult>> logger,
    IConfiguration configuration,
    ICacheService cacheService,
    IContractKeyProvider contractKeyProvider
) 
: IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = contractKeyProvider.GetContractKey(context.Message!);
        // TODO: ensure wait if key is already being requested, if item was just put in cache, don't force a flush even if one is requested?
        
        var config = this.GetItemConfig(context, (TRequest)context.Message);
        if (config == null)
            return await next().ConfigureAwait(false);

        TResult result = default!;
        if (context.HasForceCacheRefresh())
        {
            logger.LogDebug("Cache Forced Refresh - {Request}", context.Message);
            result = await next().ConfigureAwait(false);
            
            if (result != null)
            {
                var entry = await cacheService.Set(cacheKey, result, config).ConfigureAwait(false);
                context.Cache(new CacheContext(cacheKey, false, entry.CreatedAt, config));
            }
        }
        else
        {
            var hit = true;
            var entry = await cacheService
                .GetOrCreate(
                    cacheKey,
                    () =>
                    {
                        hit = false;
                        return next();
                    },
                    config
                )
                .ConfigureAwait(false)!;

            logger.LogDebug("Cache Hit: {Hit} - {Request} - Key: {RequestKey}", hit, context.Message, cacheKey);
            context.Cache(new CacheContext(cacheKey, hit, entry!.CreatedAt, config));
			result = entry.Value;
        }
        return result!;
    }


    protected virtual CacheItemConfig? GetItemConfig(IMediatorContext context, TRequest request)
    {
        // context #1
        var cache = context.TryGetCacheConfig();
        if (cache != null)
            return cache;
        
        // config #2
        var section = configuration.GetHandlerSection("Cache", context.Message!, context.MessageHandler);
        if (section != null)
        {
            var absoluteExpirationSeconds = section.GetValue("AbsoluteExpirationSeconds", 60);
            var slidingExpirationSeconds = section.GetValue("SlidingExpirationSeconds", 0);
            
            return FromSeconds(absoluteExpirationSeconds, slidingExpirationSeconds);
        }
        
        // handler attribute #3
        var attribute = ((IRequestHandler<TRequest, TResult>)context.MessageHandler).GetHandlerHandleMethodAttribute<TRequest, TResult, CacheAttribute>();
        if (attribute != null)
            return FromSeconds(attribute.AbsoluteExpirationSeconds, attribute.SlidingExpirationSeconds);

        return null;
    }

    
    static CacheItemConfig FromSeconds(int absoluteExpirationSeconds, int slidingExpirationSeconds)
    {
        TimeSpan? absoluteExpiration = null;
        TimeSpan? slidingExpiration = null;
        if (absoluteExpirationSeconds > 0)
            absoluteExpiration = TimeSpan.FromSeconds(absoluteExpirationSeconds);
            
        if (slidingExpirationSeconds > 0)
            slidingExpiration = TimeSpan.FromSeconds(slidingExpirationSeconds);
        
        return new CacheItemConfig(absoluteExpiration, slidingExpiration);
    }
}