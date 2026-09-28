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
        // RunContinuationsAsynchronously: completing this from the UI thread must not resume the
        // caller's continuation inline on the UI thread.
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }
            try
            {
                await next().ConfigureAwait(false);
                tcs.TrySetResult();
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }
}