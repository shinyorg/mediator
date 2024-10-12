using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator
{
    public async Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var context = await this
            .RequestCore<IRequest<TResult>, TResult>(request, cancellationToken)
            .ConfigureAwait(false);

        return context.Result;
    }


    public Task<ExecutionResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    ) => this.RequestCore<IRequest<TResult>, TResult>(request, cancellationToken);
    

    public Task<ExecutionContext> Send(IRequest request, CancellationToken cancellationToken = default)
        => this.SendCore(request, cancellationToken);

    
    async Task<ExecutionContext> SendCore<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest
    {
        using var scope = services.CreateScope();
        var requestHandler = scope.ServiceProvider.GetService<IRequestHandler<TRequest>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new RequestHandlerDelegate<Unit>(async () =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            await requestHandler.Handle(request, cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        });
    
        var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        await this
            .Execute(
                scope.ServiceProvider, 
                context,
                handlerExec 
            )
            .ConfigureAwait(false);
    
        return context;
    }
    
    
    async Task<ExecutionResult<TResult>> RequestCore<TRequest, TResult>(
        TRequest request, 
        CancellationToken cancellationToken
    ) where TRequest : IRequest<TResult>
    {
        using var scope = services.CreateScope();
        var requestHandler = scope.ServiceProvider.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TRequest>>();
        var handlerExec = new RequestHandlerDelegate<TResult>(() =>
        {
            logger.LogDebug(
                "Executing request handler {RequestHandlerType}", 
                requestHandler.GetType().FullName 
            );
            return requestHandler.Handle(request, cancellationToken);
        });
    
        var context = new ExecutionContext<TRequest>(request, requestHandler, cancellationToken);
        var result = await this
            .Execute(scope.ServiceProvider, context, handlerExec)
            .ConfigureAwait(false);
        
        if (result is IEvent @event)
            await this.Publish(@event, cancellationToken).ConfigureAwait(false);
        
        return new ExecutionResult<TResult>(context, result);
    }
    
    
    async Task<TResult> Execute<TRequest, TResult>(
        IServiceProvider scope, 
        ExecutionContext<TRequest> context,
        RequestHandlerDelegate<TResult> handlerExec
    )
    {
        var middlewares = scope.GetServices<IRequestMiddleware<TRequest, TResult>>();
        var logger = scope.GetRequiredService<ILogger<TRequest>>();
        
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
                    
                    return middleware.Process(context, next);
                })
            .Invoke()
            .ConfigureAwait(false);
    
        return result;
    }
}