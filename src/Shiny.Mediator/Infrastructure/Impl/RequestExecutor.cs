using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class RequestExecutor(IServiceProvider services) : IRequestExecutor
{
    public virtual async Task<RequestResult<TResult>> RequestWithContext<TResult>(
        MediatorContext context,
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
    Task<RequestResult<TResult>> Handle();
}
public class RequestResultWrapper<TRequest, TResult>(
    MediatorContext context, 
    TRequest request,
    CancellationToken cancellationToken
) : IRequestResultWrapper<TResult> where TRequest : IRequest<TResult>
{
    public async Task<RequestResult<TResult>> Handle()
    {
        var services = context.ServiceScope.ServiceProvider;
        var requestHandler = services.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        context.MessageHandler = requestHandler;
        var middlewares = context.BypassMiddlewareEnabled() ? [] : services.GetServices<IRequestMiddleware<TRequest, TResult>>();
        var logger = services.GetRequiredService<ILogger<TRequest>>();
        
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            // TODO: telemetry
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            return requestHandler.Handle((TRequest)context.Message, context, cancellationToken);
        });
        
        var result = await middlewares
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
                {
                    // TODO: telemetry
                    logger.LogDebug(
                        "Executing request middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );
                    
                    return middleware.Process(context, next, cancellationToken);
                }
            )
            .Invoke()
            .ConfigureAwait(false);
        
        return new RequestResult<TResult>(context, result);
    }
}