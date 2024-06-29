namespace Shiny.Mediator.Middleware;

public record FlushAllStoresEvent : IEvent;


public static class MiddlewareExtensions
{
    /// <summary>
    /// All middleware within Shiny will respond to this event
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushAllStores(this IMediator mediator, CancellationToken cancellationToken = default) 
        => mediator.Publish(new FlushAllStoresEvent(), cancellationToken);
}