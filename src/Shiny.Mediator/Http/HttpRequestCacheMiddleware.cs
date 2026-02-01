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
        // TODO: I only want this for HTTP Requests
        // TODO: only if BaseHttpRequestHandler? context.MessageHandler
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
/*
           var config = this.GetItemConfig(context, (TRequest)context.Message);
           if (config == null)
               return await next().ConfigureAwait(false);

           var cacheKey = contractKeyProvider.GetContractKey(context.Message!);
           TResult result = default!;

           if (context.HasForceCacheRefresh())
           {
               logger.LogDebug("Cache Forced Refresh - {Request}", context.Message);
               result = await next().ConfigureAwait(false);

               if (result != null)
               {
                   var entry = await cacheService.Set(cacheKey, result, config).ConfigureAwait(false);
                   context.Cache(new CacheContext(cacheKey, false, entry.CreatedAt, config));
               }
           }
           else
           {
               var hit = true;
               var entry = await cacheService
                   .GetOrCreate(
                       cacheKey,
                       () =>
                       {
                           hit = false;
                           return next();
                       },
                       config
                   )
                   .ConfigureAwait(false)!;

               logger.LogDebug("Cache Hit: {Hit} - {Request} - Key: {RequestKey}", hit, context.Message, cacheKey);
               context.Cache(new CacheContext(cacheKey, hit, entry!.CreatedAt, config));
   			result = entry.Value;
           }
           return result!;
       }


       [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
       protected virtual CacheItemConfig? GetItemConfig(IMediatorContext context, TRequest request)
       {
           // context #1
           var cache = context.TryGetCacheConfig();
           if (cache != null)
               return cache;

           // config #2
           var section = configuration.GetHandlerSection("Cache", context.Message!, context.MessageHandler);
           if (section != null)
           {
               var absoluteExpirationSeconds = section.GetValue("AbsoluteExpirationSeconds", 60);
               var slidingExpirationSeconds = section.GetValue("SlidingExpirationSeconds", 0);

               return FromSeconds(absoluteExpirationSeconds, slidingExpirationSeconds);
           }

           // handler attribute #3
           var attribute = context.GetHandlerAttribute<CacheAttribute>();
           if (attribute != null)
               return FromSeconds(attribute.AbsoluteExpirationSeconds, attribute.SlidingExpirationSeconds);

           return null;
       }


       static CacheItemConfig FromSeconds(int absoluteExpirationSeconds, int slidingExpirationSeconds)
       {
           TimeSpan? absoluteExpiration = null;
           TimeSpan? slidingExpiration = null;
           if (absoluteExpirationSeconds > 0)
               absoluteExpiration = TimeSpan.FromSeconds(absoluteExpirationSeconds);

           if (slidingExpirationSeconds > 0)
               slidingExpiration = TimeSpan.FromSeconds(slidingExpirationSeconds);

           return new CacheItemConfig(absoluteExpiration, slidingExpiration);
       }
   }
 */