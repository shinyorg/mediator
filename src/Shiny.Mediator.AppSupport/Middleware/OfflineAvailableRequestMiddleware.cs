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
        var acrossSessions = this.IsAcrossSessions(requestHandler, request);
        if (typeof(TResult) == typeof(Unit) || acrossSessions == null)
            return await next().ConfigureAwait(false);

        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            result = await next().ConfigureAwait(false);
            if (result != null)
                await storage.Store(request!, result, acrossSessions.Value);
        }
        else
        {
            result = await storage.Get<TResult>(request!, acrossSessions.Value);
        }
        return result;
    }


    bool? IsAcrossSessions(IRequestHandler requestHandler, TRequest? request)
    {
        bool? acrossSessions = null;
        var section = configuration.GetHandlerSection("Offline", request!, requestHandler);
        if (section == null)
        {
            var cfg = requestHandler.GetHandlerHandleMethodAttribute<TRequest, OfflineAvailableAttribute>();
            cfg ??= request!.GetType().GetCustomAttribute<OfflineAvailableAttribute>();
            if (cfg != null)
                acrossSessions = cfg.AvailableAcrossSessions;
        }
        else
        {
            acrossSessions = section.GetValue("AvailableAcrossSessions", true);
        }

        return acrossSessions;
    }
}