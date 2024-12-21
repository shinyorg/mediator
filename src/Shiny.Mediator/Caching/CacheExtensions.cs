using Shiny.Mediator.Caching;
using Shiny.Mediator.Caching.Infrastructure;

namespace Shiny.Mediator;


public static class CacheExtensions
{
    public static ShinyConfigurator AddCaching<TCache>(this ShinyConfigurator cfg) where TCache : class, ICacheService
    {
        cfg.Services.AddSingletonAsImplementedInterfaces<TCache>();
        cfg.Services.AddSingletonAsImplementedInterfaces<FlushStoreEventHandlers>();
        cfg.AddOpenRequestMiddleware(typeof(CachingRequestMiddleware<,>));
        return cfg;
    }
    
    
    /// <summary>
    /// Gets a cache context if the cache middleware was involved
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static CacheContext? Cache(this ExecutionContext context)
        => context.TryGetValue<CacheContext>("Cache");
    
    /// <summary>
    /// Meant to be used by middleware
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cacheContext"></param>
    public static void Cache(this ExecutionContext context, CacheContext cacheContext)
        => context.Add("Cache", cacheContext);
}