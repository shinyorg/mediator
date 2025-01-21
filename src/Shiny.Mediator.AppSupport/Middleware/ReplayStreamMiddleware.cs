using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Caching;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Replays the last result before requesting a new one
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public class ReplayStreamMiddleware<TRequest, TResult>(
    ILogger<ReplayStreamMiddleware<TRequest, TResult>> logger,
    IInternetService internet,
    IConfiguration configuration,
    IOfflineService? offline,
    ICacheService? cache
) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        RequestContext<TRequest> context,
        StreamRequestHandlerDelegate<TResult> next 
    )
    {
        if (!this.IsEnabled(context.Request, context.RequestHandler))
            return next();

        logger.LogDebug("ReplayStream Enabled - {Request}", context.Request);
        return this.Iterate(
            context.Request, 
            next, 
            context.CancellationToken
        );
    }


    protected bool IsEnabled(TRequest request, IRequestHandler requestHandler)
    {
        var section = configuration.GetHandlerSection("ReplayStream", request, requestHandler);
        var enabled = false;
        
        if (section == null)
        {
            enabled = requestHandler.GetHandlerHandleMethodAttribute<TRequest, ReplayStreamAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }

    
    // TODO: add cache
    protected virtual async IAsyncEnumerable<TResult> Iterate(
        TRequest request, 
        StreamRequestHandlerDelegate<TResult> next, 
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var requestKey = ContractUtils.GetObjectKey(request);
        
        if (cache != null)
        {
            var item = await cache.Get<TResult>(requestKey).ConfigureAwait(false);
            if (item != null)
                yield return item.Value;
        }
        else if (offline != null)
        {
            var store = await offline.Get<TResult>(request);
            if (store != null) // TODO: I need context here to ship out date
                yield return store.Value;
        }

        if (!internet.IsAvailable)
            await internet.WaitForAvailable(ct).ConfigureAwait(false);
        
        var nxt = this.TryNext(next, ct);
        if (nxt != null)
        {
            while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
            {
                if (cache != null)
                    await cache.Set(requestKey, nxt).ConfigureAwait(false);
                
                if (offline != null)
                    await offline.Set(request, nxt.Current!).ConfigureAwait(false);

                yield return nxt.Current;
            }
        }
    }

    IAsyncEnumerator<TResult>? TryNext(
        StreamRequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            return next().GetAsyncEnumerator(cancellationToken);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "ReplayStream Timeout");
            return null;
        }
    }
}