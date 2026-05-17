using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching.Infrastructure;


/// <summary>
/// Event handlers that translate <see cref="FlushAllStoresEvent"/> and <see cref="FlushStoresEvent"/> publications
/// into operations on the registered <see cref="ICacheService"/>.
/// </summary>
public class FlushStoreEventHandlers(ICacheService cache) :
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoresEvent>
{
    /// <inheritdoc/>
    public Task Handle(FlushAllStoresEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => cache.Clear(cancellationToken);

    // TODO: flush store by request?
    /// <inheritdoc/>
    public Task Handle(FlushStoresEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => cache.Remove(@event.RequestKey, @event.PartialMatch, cancellationToken);
}