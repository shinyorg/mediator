using Microsoft.Extensions.Caching.Memory;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Caching.Infrastructure;


public class FlushCacheEventHandler(IMemoryCache cache) : IEventHandler<FlushAllStoresEvent>
{
    public Task Handle(FlushAllStoresEvent @event, CancellationToken cancellationToken)
    {
        cache.Clear();
        return Task.CompletedTask;
    }
}