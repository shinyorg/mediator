using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Impl;


static class RequestExecutor
{
    public static async Task<TResult> Execute<TRequest, TResult>(
        IServiceProvider services, 
        ExecutionContext<TRequest> context,
        RequestHandlerDelegate<TResult> handlerExec
    )
    {
        var middlewares = services.GetServices<IRequestMiddleware<TRequest, TResult>>();
        var logger = services.GetRequiredService<ILogger<TRequest>>();
        
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
                    
                    return middleware.Process(context, next);
                })
            .Invoke()
            .ConfigureAwait(false);

        return result;
    }
}