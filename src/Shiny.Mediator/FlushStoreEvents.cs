namespace Shiny.Mediator;

public record FlushAllStoresEvent : IEvent;
public record FlushStoreByRequestEvent(object Request) : IEvent;
public record FlushStoresEvent(Type? Type = null, string? KeyPrefix = null) : IEvent;

public static class FlushStoreExtensions
{
    /// <summary>
    /// All middleware within Shiny will respond to this event
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushAllStores(this IMediator mediator, CancellationToken cancellationToken = default) 
        => mediator.Publish(new FlushAllStoresEvent(), cancellationToken);
    
    
    /// <summary>
    /// All middleware within Shiny will respond to this event
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushStoresByRequest(this IMediator mediator, object request, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoreByRequestEvent(request), cancellationToken);
    
    
    /// <summary>
    /// Flushes store by type and/or keys starting with prefix
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="type"></param>
    /// <param name="keyPrefix"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushStores(this IMediator mediator, Type? type = null, string? keyPrefix = null, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoresEvent(type, keyPrefix), cancellationToken);
}