using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


public class FlushStoreEventHandlers(ICacheService cache) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoreByRequestEvent>,
    IEventHandler<FlushStoresEvent>
{
    public Task Handle(FlushAllStoresEvent @event, EventContext<FlushAllStoresEvent> context, CancellationToken cancellationToken)
        => cache.Remove();

    public Task Handle(FlushStoreByRequestEvent @event, EventContext<FlushStoreByRequestEvent> context, CancellationToken cancellationToken)
    {
        var requestkey = Utils.GetRequestKey(@event.Request);
        return cache.RemoveByKey(requestkey);
    }

    public Task Handle(FlushStoresEvent @event, EventContext<FlushStoresEvent> context, CancellationToken cancellationToken)
        => cache.Remove(@event.Type);
}