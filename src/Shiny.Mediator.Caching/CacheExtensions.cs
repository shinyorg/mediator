using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Caching.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class CacheExtensions
{
    public static ShinyConfigurator AddMemoryCaching(this ShinyConfigurator cfg, Action<MemoryCacheOptions>? configureCache = null)
    {
        cfg.Services.AddMemoryCache(x => configureCache?.Invoke(x));
        cfg.Services.AddSingleton<IEventHandler<FlushAllStoresEvent>, FlushCacheEventHandler>();
        cfg.AddOpenRequestMiddleware(typeof(CachingRequestMiddleware<,>));
        return cfg;
    }

    public static void Clear(this IMemoryCache cache) 
        => (cache as MemoryCache)?.Clear();
}