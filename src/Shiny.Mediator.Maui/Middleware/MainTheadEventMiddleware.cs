namespace Shiny.Mediator.Middleware;

/// <summary>
/// MAUI event middleware that, when the target event handler is decorated with
/// <see cref="MainThreadAttribute"/>, marshals invocation onto the UI thread via
/// <see cref="MainThread.BeginInvokeOnMainThread(Action)"/>.
/// </summary>
[MiddlewareOrder(100)]
public class MainTheadEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    /// <inheritdoc/>
    public async Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var attr = context.GetHandlerAttribute<MainThreadAttribute>();
        
        if (attr == null)
        {
            await next().ConfigureAwait(false);
        }
        else
        {
            // RunContinuationsAsynchronously: completing this from the UI thread must not resume the
            // publisher (and the rest of the handler fan-out) inline on the UI thread.
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
            await tcs.Task.ConfigureAwait(false);
        }
    }
}