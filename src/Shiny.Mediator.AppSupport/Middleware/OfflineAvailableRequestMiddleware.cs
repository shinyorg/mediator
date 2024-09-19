using System.Reflection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableRequestMiddleware<TRequest, TResult>(
    IInternetService connectivity, 
    IStorageService storage,
    IFeatureService features
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

        var cfg = requestHandler.GetHandlerHandleMethodAttribute<TRequest, OfflineAvailableAttribute>();
        cfg ??= request!.GetType().GetCustomAttribute<OfflineAvailableAttribute>();
        if (cfg == null)
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            result = await next().ConfigureAwait(false);
            if (result != null)
                await storage.Store(request!, result, cfg.AvailableAcrossSessions);
        }
        else
        {
            result = await storage.Get<TResult>(request, cfg.AvailableAcrossSessions);
        }
        return result;
    }
}