using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Caching;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Stream request middleware that yields the most recent cached or offline-stored value to the subscriber
/// before awaiting a live result from the handler. Activated by <see cref="ReplayStreamAttribute"/> on the
/// handler method or by a <c>ReplayStream</c> configuration section. When connected, every yielded value is
/// also written back to the cache and/or offline store.
/// </summary>
public class ReplayStreamMiddleware<TRequest, TResult>(
    IInternetService internet,
    IContractKeyProvider contractKeyProvider,
    IOfflineService? offline = null,
    ICacheService? cache = null,
    ILogger<ReplayStreamMiddleware<TRequest, TResult>>? logger = null,
    IConfiguration? configuration = null
) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> Process(
        IMediatorContext context,
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        if (!this.IsEnabled(context))
            return next();

        logger?.LogDebug("Enabled - {Request}", context.Message);
        return this.Iterate(
            (TRequest)context.Message,
            context,
            next, 
            cancellationToken
        );
    }


    /// <summary>
    /// Returns <c>true</c> when replay should run for the current context. Resolves the enabled flag from the
    /// <c>ReplayStream</c> configuration section or, if absent, the presence of <see cref="ReplayStreamAttribute"/>
    /// on the handler.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Get will not be trimmed")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Get will not be trimmed")]
    protected bool IsEnabled(IMediatorContext context)
    {
        var section = context.GetHandlerSection(configuration, "ReplayStream");
        var enabled = false;
        
        if (section == null)
        {
            enabled = context.GetHandlerAttribute<ReplayStreamAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }

    
    /// <summary>
    /// Iterates the stream pipeline: first yields any cached or offline value for the request, optionally waits
    /// for the network, then yields live values from the handler while updating the cache and/or offline store.
    /// Override to customize replay/store semantics.
    /// </summary>
    protected virtual async IAsyncEnumerable<TResult> Iterate(
        TRequest request,
        IMediatorContext context,
        StreamRequestHandlerDelegate<TResult> next,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var requestKey = contractKeyProvider.GetContractKey(request);
        
        if (cache != null)
        {
            // TODO: force refresh?
            var item = await cache.Get<TResult>(requestKey, ct).ConfigureAwait(false);
            if (item == null)
            {
                logger?.LogDebug("Cache Miss - {Request}", context.Message);
            }
            else
            {
                logger?.LogDebug("Cache Hit - {Request}", context.Message);
                context.Cache(new CacheContext(item.Key, true, item.CreatedAt));
                yield return item.Value;
            }
        }
        else if (offline != null)
        {
            var store = await offline.Get<TResult>(request, ct).ConfigureAwait(false);
            if (store == null)
            {
                logger?.LogDebug("Offline Miss - {Request}", context.Message);
            }
            else
            {
                logger?.LogDebug("Offline Hit - {Request}", context.Message);
                context.Offline(new OfflineAvailableContext(requestKey, store.Timestamp));
                yield return store.Value;
            }
        }

        if (!internet.IsAvailable)
        {
            logger?.LogDebug("Waiting for internet connection- {Request}", context.Message);
            await internet.WaitForAvailable(ct).ConfigureAwait(false);
        }

        logger?.LogDebug("Internet Detected - Running Handler - {Request}", context.Message);
        var nxt = this.TryNext(next, ct);
        if (nxt != null)
        {
            try
            {
                while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
                {
                    if (cache != null)
                    {
                        logger?.LogDebug("Updating Cache - {Request}", context.Message);
                        await cache.Set(requestKey, nxt.Current!).ConfigureAwait(false);
                    }

                    if (offline != null)
                    {
                        logger?.LogDebug("Updating Offline Store - {Request}", context.Message);
                        await offline.Set(request, nxt.Current!, ct).ConfigureAwait(false);
                    }

                    logger?.LogDebug("Yielding Final Result - {Request}", context.Message);
                    yield return nxt.Current;
                }
            }
            finally
            {
                await nxt.DisposeAsync().ConfigureAwait(false);
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
            logger?.LogWarning(ex, "Handler Timeout");
            return null;
        }
    }
}