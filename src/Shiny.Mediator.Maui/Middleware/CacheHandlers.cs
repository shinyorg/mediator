using Shiny.Mediator.Maui.Services;

namespace Shiny.Mediator.Middleware;

public class CacheHandlers(CacheManager cacheManager) : IRequestHandler<FlushAllCacheRequest>, IRequestHandler<FlushCacheItemRequest>
{
    public Task Handle(FlushAllCacheRequest request, CancellationToken cancellationToken)
    {
        cacheManager.FlushAllCache();
        return Task.CompletedTask;
    }

    public Task Handle(FlushCacheItemRequest request, CancellationToken cancellationToken)
    {
        cacheManager.RemoveCacheItem(request.Request);
        return Task.CompletedTask;
    }    
}