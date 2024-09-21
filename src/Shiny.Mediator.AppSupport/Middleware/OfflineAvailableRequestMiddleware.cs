using System.Reflection;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableRequestMiddleware<TRequest, TResult>(
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

        var acrossSessions = true;
        var section = configuration.GetHandlerSection("Offline", request!, requestHandler);
        if (section == null)
        {
            var cfg = requestHandler.GetHandlerHandleMethodAttribute<TRequest, OfflineAvailableAttribute>();
            cfg ??= request!.GetType().GetCustomAttribute<OfflineAvailableAttribute>();
            if (cfg == null)
                return await next().ConfigureAwait(false);
            
            acrossSessions = cfg.AvailableAcrossSessions;
        }
        else
        {
            acrossSessions = section.GetValue("AvailableAcrossSessions", acrossSessions);
        }

        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            result = await next().ConfigureAwait(false);
            if (result != null)
                await storage.Store(request!, result, acrossSessions);
        }
        else
        {
            result = await storage.Get<TResult>(request!, acrossSessions);
        }
        return result;
    }
}