using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineFlushEventHandlers(IOfflineService offline) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoreByRequestEvent>,
    IEventHandler<FlushStoreByTypeEvent>
{
    public Task Handle(FlushAllStoresEvent @event, CancellationToken cancellationToken)
        => offline.Clear();
    
    public Task Handle(FlushStoreByRequestEvent @event, CancellationToken cancellationToken)
        => offline.ClearByRequest(@event.Request);

    public Task Handle(FlushStoreByTypeEvent @event, CancellationToken cancellationToken)
        => offline.ClearByType(@event.Type);
}