using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


public class CachingRequestMiddleware<TRequest, TResult>(
    IConfiguration configuration,
    IMemoryCache cache
) : IRequestMiddleware<TRequest, TResult>
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

        CacheAttribute? attribute = null;
        var cacheKey = CacheExtensions.GetCacheKey(request!);
        var section = configuration.GetHandlerSection("Cache", request!, requestHandler);
        
        if (section != null)
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
        else
        {
            attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, CacheAttribute>();
            attribute ??= request!.GetType().GetCustomAttribute<CacheAttribute>();
        }

        if (attribute == null && request is not ICacheControl)
            return await next().ConfigureAwait(false);

        TResult result = default!;
        if (request is ICacheControl { ForceRefresh: true })
        {
            result = await next().ConfigureAwait(false);
            var entry = cache.CreateEntry(cacheKey);
            entry.Value = result;
            this.SetCacheEntry(attribute, request, entry);
        }
        else
        {
            result = await cache
                .GetOrCreateAsync<TResult>(
                    cacheKey,
                    entry =>
                    {
                        this.SetCacheEntry(attribute, request, entry);
                        return next();
                    }
                )
                .ConfigureAwait(false)!;
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