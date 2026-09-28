using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;

/// <summary>
/// MAUI request middleware that, when the target request handler is decorated with
/// <see cref="MainThreadAttribute"/>, marshals invocation onto the UI thread via
/// <see cref="MainThread.BeginInvokeOnMainThread(Action)"/> and returns its result.
/// </summary>
[MiddlewareOrder(100)]
public class MainThreadRequestMiddleware<TRequest, TResult>(
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
        // RunContinuationsAsynchronously: completing this from the UI thread must not resume the
        // caller's continuation inline on the UI thread.
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }
            try
            {
                var nextResult = await next().ConfigureAwait(false);
                tcs.TrySetResult(nextResult);
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