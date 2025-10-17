using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class MainThreadRequestMiddleware<TRequest, TResult>(
    ILogger<MainThreadRequestMiddleware<TRequest, TResult>> logger
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public Task<TResult> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var attr = context.GetHandlerAttribute<MainThreadAttribute>();
        if (attr == null)
            return next();

        logger.LogDebug("MainThread Enabled - {Request}", context.Message);
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