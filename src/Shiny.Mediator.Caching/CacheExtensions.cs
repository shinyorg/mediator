using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Caching;

namespace Shiny.Mediator;


public static class CacheExtensions
{
    public static ShinyConfigurator AddMemoryCaching(this ShinyConfigurator cfg, Action<MemoryCacheOptions> configureCache)
    {
        cfg.Services.AddMemoryCache(configureCache);
        cfg.AddOpenRequestMiddleware(typeof(CachingRequestMiddleware<,>));
        return cfg;
    }
}