using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class MainThreadRequestHandler<TRequest, TResult> : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        var attr = requestHandler.GetHandlerHandleMethodAttribute<TRequest, MainThreadAttribute>();
        TResult result = default!;
        
        if (attr != null)
        {
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
            result = await tcs.Task.ConfigureAwait(false);
        }
        else
        {
            result = await next().ConfigureAwait(false);
        }

        return result;
    }
}