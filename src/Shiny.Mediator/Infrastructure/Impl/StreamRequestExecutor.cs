using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class StreamRequestExecutor : IStreamRequestExecutor
{
    public virtual RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        MediatorContext context,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken
    )
    {
        var wrapperType = typeof(StreamRequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);

        var wrapper = (IStreamRequestWrapper<TResult>)ActivatorUtilities.CreateInstance(
            context.ServiceScope.ServiceProvider,
            wrapperType,
            [context, request, cancellationToken]
        );
        var execution = wrapper.Handle();
        return execution;
    }
}

public interface IStreamRequestWrapper<TResult>
{
    RequestResult<IAsyncEnumerable<TResult>> Handle();
}

public class StreamRequestWrapper<TRequest, TResult>(
    MediatorContext context,
    TRequest request,
    CancellationToken cancellationToken
) : IStreamRequestWrapper<TResult> where TRequest : IStreamRequest<TResult>
{
    public RequestResult<IAsyncEnumerable<TResult>> Handle()
    {
        var services = context.ServiceScope.ServiceProvider;
        var requestHandler = services.GetService<IStreamRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        var logger = context.ServiceScope.ServiceProvider.GetRequiredService<ILogger<TRequest>>();
        
        var handlerExec = new StreamRequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing streaming request handler {RequestHandlerType}",
                requestHandler.GetType().FullName
            );
            return requestHandler.Handle(request, context, cancellationToken);
        });
        
        var middlewares = context.BypassMiddlewareEnabled() ? [] : services.GetServices<IStreamRequestMiddleware<TRequest, TResult>>();
        var enumerable = middlewares
            .Reverse()
            .Aggregate(
                handlerExec,
                (next, middleware) => () =>
                {
                    // TODO: telemetry
                    logger.LogDebug(
                        "Executing stream middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );
                    return middleware.Process(
                        context,
                        next,
                        cancellationToken
                    );
                }
            )
            .Invoke();
        
        // TODO: scope can't die until the enumerable is done - how to handle this?
        return new RequestResult<IAsyncEnumerable<TResult>>(context, enumerable);
    }
}