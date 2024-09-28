namespace Shiny.Mediator.Middleware;

public record FlushStoreByRequestKeyEvent(IRequestKey requestKey) : IEvent;


public static partial class MiddlewareExtensions
{
    /// <summary>
    /// All middleware within Shiny will respond to this event
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task FlushStoreByRequest(this IMediator mediator, IRequestKey requestKey, CancellationToken cancellationToken = default)
        => mediator.Publish(new FlushStoreByRequestKeyEvent(requestKey), cancellationToken);
}