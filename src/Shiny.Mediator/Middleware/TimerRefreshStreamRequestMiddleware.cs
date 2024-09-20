using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

public class TimerRefreshStreamRequestMiddleware<TRequest, TResult>(
    IConfiguration config
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
        var refreshSeconds = 0;
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, TimerRefreshAttribute>();
        if (attribute != null)
        {
            refreshSeconds = attribute.RefreshSeconds;
        }
        else
        {
            var section = config.GetHandlerSection(request, this);
            if (section.Exists())
            {
                refreshSeconds = section.GetValue<int>("RefreshSeconds", 0);
            }
        }

        if (refreshSeconds <= 0)
            return next();
        
        return this.Iterate(refreshSeconds, next, cancellationToken);
    }


    async IAsyncEnumerable<TResult> Iterate(int refreshSeconds, StreamRequestHandlerDelegate<TResult> next, [EnumeratorCancellation] CancellationToken ct)
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