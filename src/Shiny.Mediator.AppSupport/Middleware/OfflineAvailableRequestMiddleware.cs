using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableRequestMiddleware<TRequest, TResult>(
    ILogger<OfflineAvailableRequestMiddleware<TRequest, TResult>> logger,
    IInternetService connectivity, 
    IOfflineService offline,
    IConfiguration configuration
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        if (!this.IsEnabled(context))
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            try
            {
                result = await next().ConfigureAwait(false);
                var requestKey = await offline.Set(context.Message!, result!, cancellationToken);
                logger.LogDebug("Offline: {Request} - Key: {RequestKey}", context.Message, requestKey);
            }
            catch (TimeoutException)
            {
                result = await this.GetOffline(context, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            result = await this.GetOffline(context, cancellationToken).ConfigureAwait(false);
        }
        return result!;
    }


    async Task<TResult?> GetOffline(IMediatorContext context, CancellationToken cancellationToken)
    {
        TResult result = default!;
        var offlineResult = await offline
            .Get<TResult>(context.Message!, cancellationToken)
            .ConfigureAwait(false);
            
        if (offlineResult != null)
        {
            context.Offline(new OfflineAvailableContext(offlineResult.RequestKey, offlineResult.Timestamp));
            result = offlineResult.Value;
            
            logger.LogDebug(
                "Offline Hit: {Request} - Timestamp: {Timestamp} - Key: {RequestKey}", 
                context.Message, 
                offlineResult.Timestamp,
                offlineResult.RequestKey
            );
        }

        return result;
    }

    
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Get will not be trimmed")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Get will not be trimmed")]
    bool IsEnabled(IMediatorContext context)
    {
        var enabled = false;
        var section = context.GetHandlerSection(configuration, "Offline");
        if (section == null)
        {
            enabled = context.GetHandlerAttribute<OfflineAvailableAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }
}