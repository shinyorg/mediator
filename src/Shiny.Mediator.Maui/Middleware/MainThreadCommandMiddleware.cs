using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;

/// <summary>
/// MAUI command middleware that, when the target command handler is decorated with
/// <see cref="MainThreadAttribute"/>, marshals invocation onto the UI thread via
/// <see cref="MainThread.BeginInvokeOnMainThread(Action)"/>.
/// </summary>
[MiddlewareOrder(100)]
public class MainThreadCommandMiddleware<TCommand>(
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
        var tcs = new TaskCompletionSource();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await next().ConfigureAwait(false);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}