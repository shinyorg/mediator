using System.Reflection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableRequestMiddleware<TRequest, TResult>(IConnectivity connectivity, IStorageManager storage) : IRequestMiddleware<TRequest, TResult>
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
        var connected = connectivity.NetworkAccess == NetworkAccess.Internet;
        if (connected)
        {
            result = await next().ConfigureAwait(false);
            if (result != null)
                storage.Store(request!, result, cfg.AvailableAcrossSessions);
        }
        else
        {
            result = storage.Get<TResult>(request, cfg.AvailableAcrossSessions);
        }
        return result;
    }
}