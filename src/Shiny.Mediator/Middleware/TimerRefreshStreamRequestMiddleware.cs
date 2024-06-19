namespace Shiny.Mediator.Middleware;

public class TimerRefreshStreamRequestMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult> 
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerator<TResult> Process(
        TRequest request, 
        StreamRequestDelegate<TResult> next, 
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, TimerRefreshAttribute>();
        if (attribute == null)
            return next();

        return Iterate(attribute, next, cancellationToken);
    }


    async IAsyncEnumerator<TResult> Iterate(TimerRefreshAttribute attribute, StreamRequestDelegate<TResult> next, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(attribute.RefreshSeconds, ct);
        
            var nxt = next();
            while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
                yield return nxt.Current;
        }
    }
}