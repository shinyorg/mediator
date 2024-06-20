using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;

static class RequestExecutor
{
    public static async Task<TResult> Execute<TRequest, TResult>(
        IServiceProvider services, 
        TRequest request, 
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
                (next, middleware) => () => middleware.Process(
                    request, 
                    next, 
                    requestHandler,
                    cancellationToken
                )
            )
            .Invoke()
            .ConfigureAwait(false);

        return result;
    }
}