using System.Runtime.CompilerServices;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

public class TimerRefreshStreamRequestMiddleware<TRequest, TResult>(
    IFeatureService features
) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        TRequest request, 
        StreamRequestHandlerDelegate<TResult> next, 
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        // var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, TimerRefreshAttribute>();
        // if (attribute == null)
        //     return next();

        return Iterate(attribute, next, cancellationToken);
    }


    async IAsyncEnumerable<TResult> Iterate(TimerRefreshAttribute attribute, StreamRequestHandlerDelegate<TResult> next, [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(attribute.RefreshSeconds, ct);
        
            var nxt = next().GetAsyncEnumerator(ct);
            while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
                yield return nxt.Current;
        }
    }
}