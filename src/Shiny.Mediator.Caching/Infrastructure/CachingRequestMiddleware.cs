using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


public class CachingRequestMiddleware<TRequest, TResult>(
    ILogger<CachingRequestMiddleware<TRequest, TResult>> logger,
    IConfiguration configuration,
    IMemoryCache cache
) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        ExecutionContext<TRequest> context,
        RequestHandlerDelegate<TResult> next 
    )
    {
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);

        CacheAttribute? attribute = null;
        var cacheKey = CacheExtensions.GetCacheKey(context.Request!);
        var section = configuration.GetHandlerSection("Cache", context.Request!, context.RequestHandler);

        if (section == null)
        {
            attribute = context.RequestHandler.GetHandlerHandleMethodAttribute<TRequest, CacheAttribute>();
        }
        else
        {
            var priority = section.GetValue("Priority", CacheItemPriority.Normal);
            var absoluteExpirationSeconds = section.GetValue("AbsoluteExpirationSeconds", 60);
            var slidingExpirationSeconds = section.GetValue("SlidingExpirationSeconds", 0);

            attribute = new CacheAttribute
            {
                Priority = priority,
                AbsoluteExpirationSeconds = absoluteExpirationSeconds,
                SlidingExpirationSeconds = slidingExpirationSeconds
            };
        }
        
        if (attribute == null && context.Request is not ICacheControl)
            return await next().ConfigureAwait(false);

        TResult result = default!;
        if (context.Request is ICacheControl { ForceRefresh: true })
        {
            logger.LogDebug("Cache Forced Refresh - {Request}", context.Request);
            result = await next().ConfigureAwait(false);
            var entry = cache.CreateEntry(cacheKey);
            entry.Value = new TimestampedResult<TResult>(DateTimeOffset.UtcNow, result);
            this.SetCacheEntry(attribute, context.Request, entry);
        }
        else
        {
            var hit = true;
            var timestampedResult = await cache
                .GetOrCreateAsync<TimestampedResult<TResult>>(
                    cacheKey,
                    async entry =>
                    {
                        hit = false;
                        this.SetCacheEntry(attribute, context.Request, entry);
                        var nextResult = await next().ConfigureAwait(false);
                        return new TimestampedResult<TResult>(DateTimeOffset.UtcNow, nextResult);
                    }
                )
                .ConfigureAwait(false)!;

            result = timestampedResult!.Value;
            if (hit)
            {
                logger.LogDebug("Cache Hit: {Request} - Key: {RequestKey}", context.Request, cacheKey);
                context.Cache(new CacheContext(cacheKey, true, timestampedResult.Timestamp));
            }
            else
            {
                logger.LogDebug("Cache Miss: {Request} - Key: {RequestKey}", context.Request, cacheKey);
                context.Cache(new CacheContext(cacheKey, false, DateTimeOffset.UtcNow));
            }
        }
        return result!;
    }

    
    protected virtual void SetCacheEntry(CacheAttribute? attribute, TRequest request, ICacheEntry entry)
    {
        if (attribute != null)
        {
            entry.Priority = attribute.Priority;
            if (attribute.AbsoluteExpirationSeconds > 0)
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(attribute.AbsoluteExpirationSeconds);

            if (attribute.SlidingExpirationSeconds > 0)
                entry.SlidingExpiration = TimeSpan.FromSeconds(attribute.SlidingExpirationSeconds);
        }

        (request as ICacheControl)?.SetEntry?.Invoke(entry);
    }
}