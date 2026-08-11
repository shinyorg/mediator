using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Base class for custom <see cref="IRequestExecutor"/> implementations. Exposes a protected
/// <see cref="Execute{TRequest,TResult}"/> helper that resolves the registered handler and runs it through
/// the ordered middleware chain - reuse it when implementing alternative dispatch strategies.
/// </summary>
public abstract class RequestExecutor : IRequestExecutor
{
    /// <inheritdoc/>
    public abstract Task<TResult> Request<TResult>(
        IMediatorContext context,
        IRequest<TResult> request,
        CancellationToken cancellationToken
    );

    /// <inheritdoc/>
    public abstract bool CanHandle<TResult>(IRequest<TResult> request);


    /// <summary>
    /// Runs the strongly-typed request through its handler and middleware. Use this from
    /// <see cref="Request{TResult}"/> overrides when you need the standard local dispatch behaviour.
    /// </summary>
    protected async Task<TResult> Execute<TRequest, TResult>(
        TRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken
    ) where TRequest : IRequest<TResult>
    {
        var services = context.ServiceScope.ServiceProvider;
        var requestHandler = services.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        context.MessageHandler = requestHandler;
        var middlewares = context.BypassMiddlewareEnabled ? [] : services.GetServices<IRequestMiddleware<TRequest, TResult>>();
        var logger = services.GetService<ILogger<TRequest>>();
        
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            using (var handlerActivity = context.StartActivity("Handler"))
            {
                logger?.LogDebug(
                    "Executing request handler {RequestHandlerType}",
                    requestHandler.GetType().FullName
                );
                return requestHandler.Handle((TRequest)context.Message, context, cancellationToken);
            }
        });
        
        var result = await MiddlewareOrderResolver.OrderMiddleware(middlewares)
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
                {
                    using (var midActivity = context.StartActivity("Middleware"))
                    {
                        logger?.LogDebug(
                            "Executing request middleware {MiddlewareType}",
                            middleware.GetType().FullName
                        );

                        return middleware.Process(context, next, cancellationToken);
                    }
                }
            )
            .Invoke()
            .ConfigureAwait(false);
        
        return result;
    }
}