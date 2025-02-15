using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator
{
    public virtual async Task<RequestResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    )
    {
        using var scope = services.CreateScope();
        var wrapperType = typeof(RequestResultWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapper = (IRequestResultWrapper<TResult>)ActivatorUtilities.CreateInstance(
            scope.ServiceProvider,
            wrapperType,
            [scope.ServiceProvider, request, headers, cancellationToken]
        );
        var execution = await wrapper.Handle().ConfigureAwait(false);
        
        if (execution.Result is IEvent @event)
            await this.Publish(@event, cancellationToken).ConfigureAwait(false);
        
        return execution;
    }
}


public interface IRequestResultWrapper<TResult>
{
    Task<RequestResult<TResult>> Handle();
}
public class RequestResultWrapper<TRequest, TResult>(
    IServiceProvider scope, 
    TRequest request,
    IEnumerable<(string Key, object Value)> headers,
    CancellationToken cancellationToken
) : IRequestResultWrapper<TResult> where TRequest : IRequest<TResult>
{
    public async Task<RequestResult<TResult>> Handle()
    {
        var requestHandler = scope.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var context = new RequestContext<TRequest>(request, requestHandler);
        context.PopulateHeaders(headers);

        var middlewares = context.BypassMiddlewareEnabled() ? [] : scope.GetServices<IRequestMiddleware<TRequest, TResult>>();
        var logger = scope.GetRequiredService<ILogger<TRequest>>();
        
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            return requestHandler.Handle(context.Request, context, cancellationToken);
        });
        
        var result = await middlewares
            .Reverse()
            .Aggregate(
                handlerExec, 
                (next, middleware) => () =>
                {
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