using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


class StreamRequestWrapper<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public ExecutionResult<IAsyncEnumerable<TResult>> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IStreamRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        var logger = services.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new StreamRequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing streaming request handler {RequestHandlerType}",
                requestHandler.GetType().FullName
            );
            return requestHandler.Handle(request, cancellationToken);
        });

        var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        var middlewares = services.GetServices<IStreamRequestMiddleware<TRequest, TResult>>();
        var enumerable = middlewares
            .Reverse()
            .Aggregate(
                handlerExec,
                (next, middleware) => () =>
                {
                    logger.LogDebug(
                        "Executing stream middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );
                    return middleware.Process(
                        context,
                        next
                    );
                }
            )
            .Invoke();
        
        return new ExecutionResult<IAsyncEnumerable<TResult>>(context, enumerable);
    }
}