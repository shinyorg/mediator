using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineFlushEventHandlers(IOfflineService offline) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoreByRequestEvent>,
    IEventHandler<FlushStoreByTypeEvent>
{
    public Task Handle(FlushAllStoresEvent @event, EventContext context, CancellationToken cancellationToken)
        => offline.Clear();
    
    public Task Handle(FlushStoreByRequestEvent @event, EventContext context, CancellationToken cancellationToken)
        => offline.ClearByRequest(@event.Request);

    public Task Handle(FlushStoreByTypeEvent @event, EventContext context, CancellationToken cancellationToken)
        => offline.ClearByType(@event.Type);
}