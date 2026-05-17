using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Event handlers that flush offline-stored results in response to the mediator-wide
/// <see cref="FlushAllStoresEvent"/> and <see cref="FlushStoresEvent"/> notifications.
/// </summary>
[MiddlewareOrder(1)]
public class OfflineFlushEventHandlers(IOfflineService offline) :
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoresEvent>
{
    /// <inheritdoc/>
    public Task Handle(
        FlushAllStoresEvent @event,
        IMediatorContext context,
        CancellationToken cancellationToken
    ) => offline.Clear(cancellationToken);


    /// <inheritdoc/>
    public Task Handle(
        FlushStoresEvent @event,
        IMediatorContext context,
        CancellationToken cancellationToken
    ) => offline.Remove(@event.RequestKey, @event.PartialMatch, cancellationToken);
}