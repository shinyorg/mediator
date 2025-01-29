using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


public class CachingRequestMiddleware<TRequest, TResult>(
    ILogger<CachingRequestMiddleware<TRequest, TResult>> logger,
    IConfiguration configuration,
    ICacheService cacheService
) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        RequestContext<TRequest> context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        CacheAttribute? attribute = null;
        var cacheKey = ContractUtils.GetObjectKey(context.Request!);
        var section = configuration.GetHandlerSection("Cache", context.Request!, context.Handler);

        if (section == null)
        {
            attribute = context.Handler.GetHandlerHandleMethodAttribute<TRequest, CacheAttribute>();
        }
        else
        {
            var absoluteExpirationSeconds = section.GetValue("AbsoluteExpirationSeconds", 60);
            var slidingExpirationSeconds = section.GetValue("SlidingExpirationSeconds", 0);

            attribute = new CacheAttribute
            {
                AbsoluteExpirationSeconds = absoluteExpirationSeconds,
                SlidingExpirationSeconds = slidingExpirationSeconds
            };
        }

        var config = this.GetItemConfig(context, attribute, context.Request);
        if (config == null)
            return await next().ConfigureAwait(false);

        TResult result = default!;
        if (context.Request is ICacheControl { ForceRefresh: true } || context.HasForceCacheRefresh())
        {
            logger.LogDebug("Cache Forced Refresh - {Request}", context.Request);
            result = await next().ConfigureAwait(false);
            if (result != null)
                await cacheService.Set(cacheKey, result, config).ConfigureAwait(false);
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

            logger.LogDebug("Cache Hit: {Hit} - {Request} - Key: {RequestKey}", hit, context.Request, cacheKey);
            context.Cache(new CacheContext(cacheKey, hit, entry!.CreatedAt));
        }
        return result!;
    }


    protected virtual CacheItemConfig? GetItemConfig(RequestContext context, CacheAttribute? attribute, TRequest request)
    {
        var cache = context.TryGetCacheConfig();
        if (cache != null)
            return cache;
        
        if (request is ICacheControl control)
        {
            return new CacheItemConfig(
                control.AbsoluteExpiration,
                control.SlidingExpiration
            );
        }
        
        if (attribute != null)
        {
            TimeSpan? absoluteExpiration = null;
            TimeSpan? slidingExpiration = null;
            if (attribute.AbsoluteExpirationSeconds > 0)
                absoluteExpiration = TimeSpan.FromSeconds(attribute.AbsoluteExpirationSeconds);
            
            if (attribute.SlidingExpirationSeconds > 0)
                slidingExpiration = TimeSpan.FromSeconds(attribute.SlidingExpirationSeconds);

            return new CacheItemConfig(absoluteExpiration, slidingExpiration);
        }

        return null;
    } 
}