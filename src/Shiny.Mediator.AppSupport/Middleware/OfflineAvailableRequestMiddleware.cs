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
{
    public async Task<TResult> Process(
        ExecutionContext<TRequest> context,
        RequestHandlerDelegate<TResult> next 
    )
    {
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);
        
        if (!this.IsEnabled(context.RequestHandler, context.Request))
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            result = await next().ConfigureAwait(false);
            var requestKey = await offline.Set(context.Request!, result!);
            logger.LogDebug("Offline: {Request} - Key: {RequestKey}", context.Request, requestKey);
        }
        else
        {
            var offlineResult = await offline.Get<TResult>(context.Request!);
            
            // TODO: offline miss to context
            if (offlineResult != null)
            {
                context.Offline(new OfflineAvailableContext(offlineResult.RequestKey, offlineResult.Timestamp));
                result = offlineResult.Value;
                logger.LogDebug(
                    "Offline Hit: {Request} - Timestamp: {Timestamp} - Key: {RequestKey}", 
                    context.Request, 
                    offlineResult.Timestamp,
                    offlineResult.RequestKey
                );
            }
        }
        return result;
    }


    bool IsEnabled(IRequestHandler requestHandler, TRequest request)
    {
        var enabled = false;
        var section = configuration.GetHandlerSection("Offline", request!, requestHandler);
        if (section == null)
        {
            enabled = requestHandler.GetHandlerHandleMethodAttribute<TRequest, OfflineAvailableAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }
}