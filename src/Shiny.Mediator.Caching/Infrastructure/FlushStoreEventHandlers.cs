using Microsoft.Extensions.Caching.Memory;

namespace Shiny.Mediator.Caching.Infrastructure;


public class FlushStoreEventHandlers(IMemoryCache cache) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoreByRequestEvent>,
    IEventHandler<FlushStoreByTypeEvent>
{
    public Task Handle(FlushAllStoresEvent @event, CancellationToken cancellationToken)
    {
        cache.Clear();
        return Task.CompletedTask;
    }

    public Task Handle(FlushStoreByRequestEvent @event, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Handle(FlushStoreByTypeEvent @event, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}