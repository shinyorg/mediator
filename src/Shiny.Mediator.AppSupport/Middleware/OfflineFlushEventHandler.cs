using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineFlushEventHandlers(IOfflineService offline) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoresEvent>
{
    public Task Handle(
        FlushAllStoresEvent @event, 
        MediatorContext context, 
        CancellationToken cancellationToken
    ) => offline.Clear();


    public Task Handle(
        FlushStoresEvent @event, 
        MediatorContext context,
        CancellationToken cancellationToken
    ) => offline.Remove(@event.RequestKey, @event.PartialMatch);
}