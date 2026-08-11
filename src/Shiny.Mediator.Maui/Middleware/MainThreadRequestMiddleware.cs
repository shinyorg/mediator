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
        var tcs = new TaskCompletionSource<TResult>();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var nextResult = await next().ConfigureAwait(false);
                tcs.SetResult(nextResult);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}