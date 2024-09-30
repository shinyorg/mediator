using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class MainThreadRequestHandler<TRequest, TResult>(
    ILogger<MainThreadRequestHandler<TRequest, TResult>> logger
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public Task<TResult> Process(
        ExecutionContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next 
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