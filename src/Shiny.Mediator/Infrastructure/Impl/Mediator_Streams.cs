using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator
{
    public IAsyncEnumerable<TResult> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var context = this.RequestWithContext(request, cancellationToken);
        return context.Result;
    }


    public ExecutionResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default
    )
    {
        var scope = services.CreateScope();
        var wrapperType = typeof(StreamRequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapper = (IStreamRequestWrapper<TResult>)Activator.CreateInstance(wrapperType, [scope.ServiceProvider, request, cancellationToken]);
        var execution = wrapper.Handle();
        return execution;
    }
}

public interface IStreamRequestWrapper<TResult>
{
    ExecutionResult<IAsyncEnumerable<TResult>> Handle();
}

public class StreamRequestWrapper<TRequest, TResult>(
    IServiceProvider scope,
    TRequest request,
    CancellationToken cancellationToken
) : IStreamRequestWrapper<TResult> where TRequest : IStreamRequest<TResult>
{
    public ExecutionResult<IAsyncEnumerable<TResult>> Handle()
    {
        var requestHandler = scope.GetService<IStreamRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        var logger = scope.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new StreamRequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing streaming request handler {RequestHandlerType}",
                requestHandler.GetType().FullName
            );
            return requestHandler.Handle(request, cancellationToken);
        });

        var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        var middlewares = scope.GetServices<IStreamRequestMiddleware<TRequest, TResult>>();
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
        
        // TODO: scope can't die until the enumerable is done - how to handle this?
        return new ExecutionResult<IAsyncEnumerable<TResult>>(context, enumerable);
    }
}