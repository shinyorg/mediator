using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class LocalStreamRequestExecutor : IStreamRequestExecutor
{
    public virtual IAsyncEnumerable<TResult> Request<TResult>(
        IMediatorContext context,
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

    public bool CanRequest<TResult>(IStreamRequest<TResult> request) => true;

    public virtual IAsyncEnumerable<TResult> Handle<TRequest, TResult>(
        IMediatorContext context,
        TRequest request,
        CancellationToken cancellationToken
    ) where TRequest : IStreamRequest<TResult> => new StreamRequestWrapper<TRequest, TResult>(context, request, cancellationToken).Handle();
}

public interface IStreamRequestWrapper<TResult>
{
    IAsyncEnumerable<TResult> Handle();
}

public class StreamRequestWrapper<TRequest, TResult>(
    IMediatorContext context,
    TRequest request,
    CancellationToken cancellationToken
) : IStreamRequestWrapper<TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Handle()
    {
        var services = context.ServiceScope.ServiceProvider;
        var requestHandler = services.GetService<IStreamRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        context.MessageHandler = requestHandler;
        var logger = context.ServiceScope.ServiceProvider.GetRequiredService<ILogger<TRequest>>();
        
        var handlerExec = new StreamRequestHandlerDelegate<TResult>(() =>
        {
            using (var handlerActivity = context.StartActivity("Handler"))
            {
                logger.LogDebug(
                    "Executing streaming request handler {RequestHandlerType}",
                    requestHandler.GetType().FullName
                );
                return requestHandler.Handle(request, context, cancellationToken);
            }
        });
        
        var middlewares = context.BypassMiddlewareEnabled ? [] : services.GetServices<IStreamRequestMiddleware<TRequest, TResult>>();
        var enumerable = middlewares
            .Reverse()
            .Aggregate(
                handlerExec,
                (next, middleware) => () =>
                {
                    using (var midActivity = context.StartActivity("Middleware"))
                    {
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
                }
            )
            .Invoke();
        
        // TODO: scope can't die until the enumerable is done - how to handle this?
            // TODO: when the enumerable stops, I need to dispose of the scope
        return enumerable;
    }
}