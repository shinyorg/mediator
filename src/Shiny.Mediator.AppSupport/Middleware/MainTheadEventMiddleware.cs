using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

/// <summary>
/// Event middleware that, when the target event handler is decorated with
/// <see cref="MainThreadAttribute"/>, marshals invocation onto the UI thread via
/// the platform <see cref="IMainThreadDispatcher"/>.
/// </summary>
[MiddlewareOrder(100)]
public class MainTheadEventMiddleware<TEvent>(IMainThreadDispatcher dispatcher) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    /// <inheritdoc/>
    public Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var attr = context.GetHandlerAttribute<MainThreadAttribute>();
        if (attr == null)
            return next();

        // the publisher (and the rest of the handler fan-out) is not resumed on the UI thread
        return dispatcher.InvokeOnMainThreadAsync(() => next(), cancellationToken);
    }
}
