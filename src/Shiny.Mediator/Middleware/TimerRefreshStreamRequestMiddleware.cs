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
        ExecutionContext<TRequest> context, 
        StreamRequestHandlerDelegate<TResult> next 
    )
    {
        var interval = 0;
        var section = configuration.GetHandlerSection("TimerRefresh", context.Request, context.RequestHandler);
        if (section != null)
        {
            interval = section.GetValue("IntervalSeconds", 0);
        }
        else
        {
            var attribute = context.RequestHandler.GetHandlerHandleMethodAttribute<TRequest, TimerRefreshAttribute>();
            if (attribute != null)
                interval = attribute.IntervalSeconds;
        }

        if (interval <= 0)
            return next();
        
        return this.Iterate(interval, next, context.CancellationToken);
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