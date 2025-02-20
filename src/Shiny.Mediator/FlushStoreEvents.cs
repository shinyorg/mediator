namespace Shiny.Mediator;

public record FlushAllStoresEvent : IEvent;
public record FlushStoresEvent(string RequestKey, bool PartialMatch) : IEvent;

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
    /// Flushes store by type and/or keys starting with prefix
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="requestKey"></param>
    /// <param name="partialMatch"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushStores(this IMediator mediator, string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoresEvent(requestKey, partialMatch), cancellationToken);
}