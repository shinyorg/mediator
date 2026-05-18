namespace Shiny.Mediator;


/// <summary>
/// Delegate representing the next step in an event middleware chain. Invoke to continue execution.
/// </summary>
public delegate Task EventHandlerDelegate();

/// <summary>
/// Middleware that wraps each <see cref="IEventHandler{TEvent}"/> invocation for an event. Order is controlled
/// by <see cref="MiddlewareOrderAttribute"/>; the chain runs once per handler.
/// </summary>
public interface IEventMiddleware<TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Wraps the event handler invocation. Call <paramref name="next"/> to proceed to the next middleware or the handler.
    /// </summary>
    Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    );
}
