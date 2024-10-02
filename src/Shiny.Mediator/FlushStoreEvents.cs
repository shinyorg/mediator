namespace Shiny.Mediator;

public record FlushAllStoresEvent : IEvent;
public record FlushStoreByRequestEvent(object Request) : IEvent;
public record FlushStoreByTypeEvent(Type Type) : IEvent;

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
    public static Task FlushStoreByRequest(this IMediator mediator, object request, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoreByRequestEvent(request), cancellationToken);
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="type"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushStoreByType(this IMediator mediator, Type type, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoreByTypeEvent(type), cancellationToken);
}