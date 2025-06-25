using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineFlushEventHandlers(IOfflineService offline) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoresEvent>
{
    public Task Handle(
        FlushAllStoresEvent @event, 
        IMediatorContext context, 
        CancellationToken cancellationToken
    ) => offline.Clear(cancellationToken);


    public Task Handle(
        FlushStoresEvent @event, 
        IMediatorContext context,
        CancellationToken cancellationToken
    ) => offline.Remove(@event.RequestKey, @event.PartialMatch, cancellationToken);
}