using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


public class FlushStoreEventHandlers(ICacheService cache) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoresEvent>
{
    public Task Handle(FlushAllStoresEvent @event, MediatorContext context, CancellationToken cancellationToken)
        => cache.Clear();

    // TODO: flush store by request?
    public Task Handle(FlushStoresEvent @event, MediatorContext context, CancellationToken cancellationToken)
        => cache.Remove(@event.RequestKey, @event.PartialMatch);
}