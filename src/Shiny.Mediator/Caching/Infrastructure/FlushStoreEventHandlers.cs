namespace Shiny.Mediator.Caching.Infrastructure;


public class FlushStoreEventHandlers(ICacheService cache) : 
    IEventHandler<FlushAllStoresEvent>,
    IEventHandler<FlushStoreByRequestEvent>,
    IEventHandler<FlushStoreByTypeEvent>
{
    public Task Handle(FlushAllStoresEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        cache.Clear();
        return Task.CompletedTask;
    }

    public Task Handle(FlushStoreByRequestEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        var requestkey = Utils.GetRequestKey(@event.Request);
        cache.Remove(requestkey);
        return Task.CompletedTask;
    }

    public Task Handle(FlushStoreByTypeEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        var t = @event.Type;
        var startsWith = $"{t.Namespace}.{t.Name}";
        cache.RemoveByPrefix(startsWith);
        return Task.CompletedTask;
    }
}