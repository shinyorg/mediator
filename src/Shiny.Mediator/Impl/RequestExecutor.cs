using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Impl;

static class RequestExecutor
{
    public static async Task<TResult> Execute<TRequest, TResult>(
        IServiceProvider services, 
        TRequest request, 
        ILogger<TRequest> logger,
        IRequestHandler requestHandler,
        RequestHandlerDelegate<TResult> handlerExec,
        CancellationToken cancellationToken
    )
    {
        var middlewares = services.GetServices<IRequestMiddleware<TRequest, TResult>>();
        
        var result = await middlewares
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
                {
                    logger.LogDebug(
                        "Executing request middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );
                    
                    return middleware.Process(
                        request,
                        next,
                        requestHandler,
                        cancellationToken
                    );
                })
            .Invoke()
            .ConfigureAwait(false);

        return result;
    }
}