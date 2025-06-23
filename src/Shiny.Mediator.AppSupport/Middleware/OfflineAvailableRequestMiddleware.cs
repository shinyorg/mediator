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
        if (!this.IsEnabled(context.MessageHandler, context.Message))
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
        return result;
    }


    async Task<TResult?> GetOffline(IMediatorContext context, CancellationToken cancellationToken)
    {
        TResult result = default;
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

    bool IsEnabled(object requestHandler, object request)
    {
        var enabled = false;
        var section = configuration.GetHandlerSection("Offline", request!, requestHandler);
        if (section == null)
        {
            enabled = ((IRequestHandler<TRequest, TResult>)requestHandler).GetHandlerHandleMethodAttribute<TRequest, TResult, OfflineAvailableAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }
}