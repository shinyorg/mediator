using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableRequestMiddleware<TRequest, TResult>(
    ILogger<OfflineAvailableRequestMiddleware<TRequest, TResult>> logger,
    IInternetService connectivity, 
    IStorageService storage,
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
        
        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            result = await next().ConfigureAwait(false);
            if (this.IsEnabled(context.RequestHandler, context.Request))
            {
                var timestampedResult = new TimestampedResult<TResult>(DateTimeOffset.UtcNow, result);
                await storage.Store(context.Request!, timestampedResult);
                logger.LogDebug("Offline Store - {Request}", context.Request);
            }
        }
        else
        {
            var timestampedResult = await storage.Get<TimestampedResult<TResult>>(context.Request!);
            context.SetOfflineTimestamp(timestampedResult!.Timestamp);
            result = timestampedResult!.Value;
            logger.LogDebug("Offline Hit: {Request} - Timestamp: {Timestamp}", context.Request, timestampedResult.Value);
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