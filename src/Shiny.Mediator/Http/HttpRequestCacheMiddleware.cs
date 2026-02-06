using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Http;


public class HttpRequestCacheMiddleware<TRequest, TResult>(
    ILogger<HttpRequestCacheMiddleware<TRequest, TResult>> logger,
    TimeProvider timeProvider,
    ICacheService cacheService,
    IContractKeyProvider contractKeyProvider
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(IMediatorContext context, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var contractKey = contractKeyProvider.GetContractKey(context.Message!);
        TResult result = default!;

        if (context.HasForceCacheRefresh())
        {
            logger.LogDebug("HTTP Cache Forced Refresh - {Request}", context.Message);
            result = await next().ConfigureAwait(false);

            if (result != null)
                await TryCacheEntry(result, context, contractKey, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            logger.LogDebug("HTTP Cache Hit Attempt - {Request} ({ContractKey})", context.Message, contractKey);
            var entry = await cacheService.Get<TResult>(contractKey, cancellationToken);
            if (entry != null)
            {
                logger.LogInformation("HTTP Cache Hit Successfully - {Request} ({ContractKey})", context.Message, contractKey);
                result = entry.Value;
            }
            else
            {
                logger.LogInformation("HTTP Cache Miss - {Request} ({ContractKey})", context.Message, contractKey);
                result = await next().ConfigureAwait(false);
                await this.TryCacheEntry(result, context, contractKey, cancellationToken).ConfigureAwait(false);
            }
        }
        return result;
    }


    protected async Task TryCacheEntry(TResult result, IMediatorContext context, string contractKey, CancellationToken cancellationToken)
    {
        var httpResponse = context.GetHttpResponse();
        if (httpResponse?.Headers.CacheControl != null)
        {
            logger.LogInformation("HTTP Cache Header Set - {Request} ({ContractKey})", context.Message, contractKey);
            var cc = httpResponse.Headers.CacheControl;

            if (cc.MaxAge == null || cc.MaxAge <= TimeSpan.Zero || cc.NoCache)
            {
                logger.LogInformation("HTTP Cache Not Cached - {Request} ({ContractKey})", context.Message, contractKey);
                return;
            }

            await cacheService.Set(
                contractKey,
                new CacheEntry<TResult>(
                    contractKey,
                    result,
                    timeProvider.GetUtcNow()
                ),
                new CacheItemConfig
                {
                    AbsoluteExpiration = cc.MaxAge!.Value
                },
                cancellationToken
            );
            logger.LogInformation(
                "HTTP Cache Set {MaxAge} - {Request} ({ContractKey})",
                cc.MaxAge,
                context.Message,
                contractKey
            );
        }
    }
}