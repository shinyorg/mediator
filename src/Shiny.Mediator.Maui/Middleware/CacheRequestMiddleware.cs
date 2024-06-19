using Shiny.Mediator.Maui.Services;

namespace Shiny.Mediator.Middleware;


public class CacheRequestMiddleware<TRequest, TResult>(CacheManager cacheManager) : IRequestMiddleware<TRequest, TResult>
{ 
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

        var result = await this.Process(cfg, request, next).ConfigureAwait(false);
        return result;
    }


    public virtual Task<TResult> Process(
        CacheAttribute cfg, 
        TRequest request, 
        RequestHandlerDelegate<TResult> next
    )
    => cacheManager.CacheOrGet(
        cfg, 
        request!, 
        async () => await next().ConfigureAwait(false)
    );
}


/// <summary>
/// Implementing this interface will allow you to create your own cache key, otherwise the cache key is based on the name
/// of the request model
/// </summary>
public interface ICacheItem
{
    string CacheKey { get; }
}

