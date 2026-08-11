using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Base class for custom <see cref="IStreamRequestExecutor"/> implementations. Exposes a virtual
/// <see cref="Execute{TRequest,TResult}"/> helper that resolves the registered stream handler and runs it
/// through the ordered middleware chain - reuse it when implementing alternative dispatch strategies.
/// </summary>
public abstract class StreamRequestExecutor : IStreamRequestExecutor
{
    /// <inheritdoc/>
    public abstract IAsyncEnumerable<TResult> Request<TResult>(
        IMediatorContext context,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken
    );

    /// <inheritdoc/>
    public abstract bool CanRequest<TResult>(IStreamRequest<TResult> request);


    /// <summary>
    /// Runs the strongly-typed stream request through its handler and middleware. Use this from
    /// <see cref="Request{TResult}"/> overrides when you need the standard local dispatch behaviour.
    /// </summary>
    public virtual IAsyncEnumerable<TResult> Execute<TRequest, TResult>(
        IMediatorContext context,
        TRequest request,
        CancellationToken cancellationToken
    ) where TRequest : IStreamRequest<TResult>
    {
        var services = context.ServiceScope.ServiceProvider;
        var requestHandler = services.GetService<IStreamRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        context.MessageHandler = requestHandler;
        var logger = context.ServiceScope.ServiceProvider.GetService<ILogger<TRequest>>();
        
        var handlerExec = new StreamRequestHandlerDelegate<TResult>(() =>
        {
            using (var handlerActivity = context.StartActivity("Handler"))
            {
                logger?.LogDebug(
                    "Executing streaming request handler {RequestHandlerType}",
                    requestHandler.GetType().FullName
                );
                return requestHandler.Handle(request, context, cancellationToken);
            }
        });
        
        var middlewares = context.BypassMiddlewareEnabled ? [] : services.GetServices<IStreamRequestMiddleware<TRequest, TResult>>();
        var enumerable = MiddlewareOrderResolver.OrderMiddleware(middlewares)
            .Reverse()
            .Aggregate(
                handlerExec,
                (next, middleware) => () =>
                {
                    using (var midActivity = context.StartActivity("Middleware"))
                    {
                        logger?.LogDebug(
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
