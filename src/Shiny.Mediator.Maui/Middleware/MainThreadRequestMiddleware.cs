using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class MainThreadRequestMiddleware<TRequest, TResult>(
    ILogger<MainThreadRequestMiddleware<TRequest, TResult>> logger
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public Task<TResult> Process(
        RequestContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var attr = context.RequestHandler.GetHandlerHandleMethodAttribute<TRequest, MainThreadAttribute>();
        TResult result = default!;

        if (attr == null)
            return next();

        logger.LogDebug("MainThread Enabled - {Request}", context.Request);
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