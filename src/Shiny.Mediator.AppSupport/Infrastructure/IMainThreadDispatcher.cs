namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Platform abstraction for running work on the UI (main) thread. Implemented by each UI host
/// (MAUI, Uno Platform, Avalonia) and used by the <see cref="MainThreadAttribute"/> middleware.
/// </summary>
public interface IMainThreadDispatcher
{
    /// <summary>
    /// <c>true</c> when the calling thread is the UI thread.
    /// </summary>
    bool IsMainThread { get; }

    /// <summary>
    /// Queues <paramref name="action"/> to run on the UI thread and returns immediately.
    /// </summary>
    void BeginInvokeOnMainThread(Action action);
}


/// <summary>
/// Helpers for <see cref="IMainThreadDispatcher"/>.
/// </summary>
public static class MainThreadDispatcherExtensions
{
    /// <summary>
    /// Runs <paramref name="func"/> on the UI thread and returns a task that completes with its
    /// result. The returned task completes asynchronously, so the caller does not resume on the
    /// UI thread. If <paramref name="cancellationToken"/> is cancelled before the UI thread picks
    /// up the work, <paramref name="func"/> is skipped and the task is cancelled.
    /// </summary>
    public static Task<T> InvokeOnMainThreadAsync<T>(
        this IMainThreadDispatcher dispatcher,
        Func<Task<T>> func,
        CancellationToken cancellationToken = default
    )
    {
        // RunContinuationsAsynchronously: completing this from the UI thread must not resume the
        // caller's continuation inline on the UI thread.
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.BeginInvokeOnMainThread(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }
            try
            {
                var result = await func().ConfigureAwait(false);
                tcs.TrySetResult(result);
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


    /// <summary>
    /// Runs <paramref name="func"/> on the UI thread. See
    /// <see cref="InvokeOnMainThreadAsync{T}(IMainThreadDispatcher, Func{Task{T}}, CancellationToken)"/>.
    /// </summary>
    public static Task InvokeOnMainThreadAsync(
        this IMainThreadDispatcher dispatcher,
        Func<Task> func,
        CancellationToken cancellationToken = default
    ) => dispatcher.InvokeOnMainThreadAsync<bool>(async () =>
    {
        await func().ConfigureAwait(false);
        return true;
    }, cancellationToken);
}
