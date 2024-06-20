using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


class StreamRequestWrapper<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IStreamRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var handlerExec = new StreamRequestHandlerDelegate<TResult>(()
            => requestHandler.Handle(request, cancellationToken));

        var middlewares = services.GetServices<IStreamRequestMiddleware<TRequest, TResult>>();

        var enumerable = middlewares
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
            .Invoke();
        
        return enumerable;
    }
}