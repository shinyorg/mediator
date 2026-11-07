using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

/// <summary>
/// Request middleware that, when the target request handler is decorated with
/// <see cref="MainThreadAttribute"/>, marshals invocation onto the UI thread via
/// the platform <see cref="IMainThreadDispatcher"/> and returns its result.
/// </summary>
[MiddlewareOrder(100)]
public class MainThreadRequestMiddleware<TRequest, TResult>(
    IMainThreadDispatcher dispatcher,
    ILogger<MainThreadRequestMiddleware<TRequest, TResult>>? logger = null
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    /// <inheritdoc/>
    public Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var attr = context.GetHandlerAttribute<MainThreadAttribute>();
        if (attr == null)
            return next();

        logger?.LogDebug("MainThread Enabled - {Request}", context.Message);
        return dispatcher.InvokeOnMainThreadAsync(() => next(), cancellationToken);
    }
}
