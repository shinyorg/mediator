using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineFlushEventHandlers(IOfflineService offline) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoreByRequestEvent>,
    IEventHandler<FlushStoreByTypeEvent>
{
    public Task Handle(
        FlushAllStoresEvent @event, 
        EventContext<FlushAllStoresEvent> context, 
        CancellationToken cancellationToken
    ) => Task.CompletedTask;
        // => offline.Clear();

    public Task Handle(
        FlushStoreByRequestEvent @event,
        EventContext<FlushStoreByRequestEvent> context,
        CancellationToken cancellationToken
    ) => Task.CompletedTask; // offline.ClearByRequest(@event.Request);

    public Task Handle(
        FlushStoreByTypeEvent @event, 
        EventContext<FlushStoreByTypeEvent> context,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;
    //=> offline.ClearByType(@event.Type);
}