using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

public class TimerRefreshStreamRequestMiddleware<TRequest, TResult>(
    IConfiguration configuration
) : IStreamRequestMiddleware<TRequest, TResult> 
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        TRequest request, 
        StreamRequestHandlerDelegate<TResult> next, 
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        var interval = 0;
        var section = configuration.GetHandlerSection("TimerRefresh", request, requestHandler);
        if (section != null)
        {
            interval = section.GetValue("IntervalSeconds", 0);
        }
        else
        {
            var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, TimerRefreshAttribute>();
            if (attribute != null)
                interval = attribute.IntervalSeconds;
        }

        if (interval <= 0)
            return next();
        
        return this.Iterate(interval, next, cancellationToken);
    }


    async IAsyncEnumerable<TResult> Iterate(
        int refreshSeconds, 
        StreamRequestHandlerDelegate<TResult> next, 
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(refreshSeconds, ct);
        
            var nxt = next().GetAsyncEnumerator(ct);
            while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
                yield return nxt.Current;
        }
    }
}