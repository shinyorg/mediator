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
        IMediatorContext context,
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        if (!this.IsEnabled(context.Message, context.MessageHandler))
            return next();

        logger.LogDebug("Enabled - {Request}", context.Message);
        return this.Iterate(
            (TRequest)context.Message,
            context,
            next, 
            cancellationToken
        );
    }


    protected bool IsEnabled(object request, object requestHandler)
    {
        var section = configuration.GetHandlerSection("ReplayStream", request, requestHandler);
        var enabled = false;
        
        if (section == null)
        {
            enabled = ((IStreamRequestHandler<TRequest, TResult>)requestHandler).GetHandlerHandleMethodAttribute<TRequest, ReplayStreamAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }

    
    protected virtual async IAsyncEnumerable<TResult> Iterate(
        TRequest request, 
        IMediatorContext context,
        StreamRequestHandlerDelegate<TResult> next, 
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var requestKey = ContractUtils.GetRequestKey(request);
        
        if (cache != null)
        {
            // TODO: force refresh?
            var item = await cache.Get<TResult>(requestKey).ConfigureAwait(false);
            if (item == null)
            {
                logger.LogDebug("Cache Miss - {Request}", context.Message);
            }
            else
            {
                logger.LogDebug("Cache Hit - {Request}", context.Message);
                context.Cache(new CacheContext(item.Key, true, item.CreatedAt));
                yield return item.Value;
            }
        }
        else if (offline != null)
        {
            var store = await offline.Get<TResult>(request);
            if (store == null)
            {
                logger.LogDebug("Offline Miss - {Request}", context.Message);
            }
            else
            {
                logger.LogDebug("Offline Hit - {Request}", context.Message);
                context.Offline(new OfflineAvailableContext(requestKey, store.Timestamp));
                yield return store.Value;
            }
        }

        if (!internet.IsAvailable)
        {
            logger.LogDebug("Waiting for internet connection- {Request}", context.Message);
            await internet.WaitForAvailable(ct).ConfigureAwait(false);
        }

        logger.LogDebug("Internet Detected - Running Handler - {Request}", context.Message);
        var nxt = this.TryNext(next, ct);
        if (nxt != null)
        {
            while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
            {
                if (cache != null)
                {
                    logger.LogDebug("Updating Cache - {Request}", context.Message);
                    await cache.Set(requestKey, nxt).ConfigureAwait(false);
                }


                if (offline != null)
                {
                    logger.LogDebug("Updating Offline Store - {Request}", context.Message);
                    await offline.Set(request, nxt.Current!).ConfigureAwait(false);
                }

                logger.LogDebug("Yielding Final Result - {Request}", context.Message);
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
            logger.LogWarning(ex, "Handler Timeout");
            return null;
        }
    }
}