using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public abstract class RequestExecutor : IRequestExecutor
{
    public abstract Task<TResult> Request<TResult>(
        IMediatorContext context,
        IRequest<TResult> request,
        CancellationToken cancellationToken
    );
    
    public abstract bool CanHandle<TResult>(IRequest<TResult> request);


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
        var logger = services.GetRequiredService<ILogger<TRequest>>();
        
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            using (var handlerActivity = context.StartActivity("Handler"))
            {
                logger.LogDebug(
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
                        logger.LogDebug(
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