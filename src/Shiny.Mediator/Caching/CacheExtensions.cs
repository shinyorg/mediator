using System.Diagnostics.CodeAnalysis;
using Shiny.Mediator.Caching;
using Shiny.Mediator.Caching.Infrastructure;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public static class CacheExtensions
{
    public static ShinyMediatorBuilder AddCaching<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.Interfaces
        )] TCache
    >(this ShinyMediatorBuilder cfg) where TCache : class, ICacheService
    {
        if (cfg.Services.Any(x => x.ServiceType == typeof(ICacheService)))
            throw new InvalidOperationException("You can only have one mediator cache service registered");
        
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
    public static CacheContext? Cache(this IMediatorContext context)
        => context.TryGetValue<CacheContext>("Cache");
    
    /// <summary>
    /// Meant to be used by middleware
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cacheContext"></param>
    public static void Cache(this IMediatorContext context, CacheContext cacheContext)
        => context.AddHeader("Cache", cacheContext);
}