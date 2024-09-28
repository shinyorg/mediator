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
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            result = await next().ConfigureAwait(false);
            if (this.IsEnabled(requestHandler, request))
            {
                await storage.Store(request!, result);
                logger.LogDebug("Cache Store - {Request}", request);
            }
        }
        else
        {
            result = await storage.Get<TResult>(request!);
            logger.LogDebug("Cache Hit - {Request}", request);
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