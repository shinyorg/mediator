using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

/// <summary>
/// Command middleware that, when the target command handler is decorated with
/// <see cref="MainThreadAttribute"/>, marshals invocation onto the UI thread via
/// the platform <see cref="IMainThreadDispatcher"/>.
/// </summary>
[MiddlewareOrder(100)]
public class MainThreadCommandMiddleware<TCommand>(
    IMainThreadDispatcher dispatcher,
    ILogger<MainThreadCommandMiddleware<TCommand>>? logger = null
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
    public Task Process(
        IMediatorContext context,
        CommandHandlerDelegate next,
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
