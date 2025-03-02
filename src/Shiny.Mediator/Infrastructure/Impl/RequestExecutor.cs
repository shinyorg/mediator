using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class RequestExecutor : IRequestExecutor
{
    public virtual async Task<TResult> Request<TResult>(
        IMediatorContext context,
        IRequest<TResult> request,
        CancellationToken cancellationToken
    )
    {
        var wrapperType = typeof(RequestResultWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapper = (IRequestResultWrapper<TResult>)ActivatorUtilities.CreateInstance(
            context.ServiceScope.ServiceProvider,
            wrapperType,
            [context, request, cancellationToken]
        );
        var execution = await wrapper.Handle().ConfigureAwait(false);
        
        return execution;
    }
}


public interface IRequestResultWrapper<TResult>
{
    Task<TResult> Handle();
}
public class RequestResultWrapper<TRequest, TResult>(
    IMediatorContext context, 
    TRequest request,
    CancellationToken cancellationToken
) : IRequestResultWrapper<TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Handle()
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
        
        var result = await middlewares
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