using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Http;


/// <summary>
/// Request middleware that caches HTTP responses keyed by contract, honoring
/// <c>Cache-Control: max-age</c> from the upstream response. Concurrent requests
/// for the same key are serialized via an internal <see cref="KeyedLocker"/> so
/// only one network round-trip happens per key while the entry is unset.
/// Opt-in registration via <c>AddHttpCacheMiddleware()</c> — not part of the
/// default middleware set.
/// </summary>
public class HttpRequestCacheMiddleware<TRequest, TResult>(
    TimeProvider timeProvider,
    ICacheService cacheService,
    IContractKeyProvider contractKeyProvider,
    ILogger<HttpRequestCacheMiddleware<TRequest, TResult>>? logger = null
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    readonly KeyedLocker locker = new();

    /// <inheritdoc/>
    public async Task<TResult> Process(IMediatorContext context, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var contractKey = contractKeyProvider.GetContractKey(context.Message!);
        TResult result = default!;

        if (context.HasForceCacheRefresh())
        {
            logger?.LogDebug("HTTP Cache Forced Refresh - {Request}", context.Message);
            result = await next().ConfigureAwait(false);

            if (result != null)
                await TryCacheEntry(result, context, contractKey, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using (await this.locker.LockAsync(contractKey, cancellationToken).ConfigureAwait(false))
            {
                logger?.LogDebug("HTTP Cache Hit Attempt - {Request} ({ContractKey})", context.Message, contractKey);
                var entry = await cacheService.Get<TResult>(contractKey, cancellationToken).ConfigureAwait(false);
                if (entry != null)
                {
                    logger?.LogInformation("HTTP Cache Hit Successfully - {Request} ({ContractKey})", context.Message, contractKey);
                    result = entry.Value;
                }
                else
                {
                    logger?.LogInformation("HTTP Cache Miss - {Request} ({ContractKey})", context.Message, contractKey);
                    result = await next().ConfigureAwait(false);
                    await this.TryCacheEntry(result, context, contractKey, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        return result;
    }


    /// <summary>
    /// Inspects the captured HTTP response for a cacheable <c>Cache-Control</c>
    /// directive and, when present, stores <paramref name="result"/> with an
    /// absolute expiration of <c>max-age</c>.
    /// </summary>
    protected async Task TryCacheEntry(TResult result, IMediatorContext context, string contractKey, CancellationToken cancellationToken)
    {
        var httpResponse = context.GetHttpResponse();
        if (httpResponse?.Headers.CacheControl != null)
        {
            logger?.LogInformation("HTTP Cache Header Set - {Request} ({ContractKey})", context.Message, contractKey);
            var cc = httpResponse.Headers.CacheControl;

            if (cc.MaxAge == null || cc.MaxAge <= TimeSpan.Zero || cc.NoCache)
            {
                logger?.LogInformation("HTTP Cache Not Cached - {Request} ({ContractKey})", context.Message, contractKey);
                return;
            }

            await cacheService.Set(
                contractKey,
                result,
                new CacheItemConfig
                {
                    AbsoluteExpiration = cc.MaxAge!.Value
                },
                cancellationToken
            );
            logger?.LogInformation(
                "HTTP Cache Set {MaxAge} - {Request} ({ContractKey})",
                cc.MaxAge,
                context.Message,
                contractKey
            );
        }
    }
}
