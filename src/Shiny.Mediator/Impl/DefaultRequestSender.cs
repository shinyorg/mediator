using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class DefaultRequestSender(IServiceProvider services) : IRequestSender
{
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken) where TRequest : IRequest
    {
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        await this.ExecuteMiddleware(
            scope, 
            (IRequest<Unit>)request, 
            async () =>
            {
                await handlers
                    .First()
                    .Handle(request, cancellationToken)
                    .ConfigureAwait(false);
                return Unit.Value;
            },
            cancellationToken
        )
        .ConfigureAwait(false);
    }
    
    
    public async Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        Func<Task<TResult>> execute = async () =>
        {
            var handler = handlers.First();
            var handleMethod = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public)!;
            var resultTask = (Task<TResult>)handleMethod.Invoke(handler, [request, cancellationToken])!;
            var result = await resultTask.ConfigureAwait(false);
            return result;
        };
        var result = await this.ExecuteMiddleware(scope, request, execute, cancellationToken).ConfigureAwait(false);
        return result;
    }


    async Task<TResult> ExecuteMiddleware<TRequest, TResult>(
        IServiceScope scope, 
        TRequest request, 
        Func<Task<TResult>> initialExecute, 
        CancellationToken cancellationToken
    ) where TRequest : IRequest<TResult>
    {
        var middlewareType = typeof(IRequestMiddleware<,>).MakeGenericType(request.GetType(), typeof(TResult));
        var middlewareMethod = middlewareType.GetMethod("Process", BindingFlags.Instance | BindingFlags.Public)!;
        var middlewares = scope.ServiceProvider.GetServices(middlewareType).ToList();

        // middlewares.Reverse();
        // foreach (var middleware in middlewares)
        // {
        //     var next = () =>
        //     {
        //         return (Task<TResult>)middlewareMethod.Invoke(middleware, [
        //             request, 
        //             next, 
        //             cancellationToken
        //         ]);
        //     };
        // }
        //
        // await next!.Invoke().ConfigureAwait(false);
        // we setup execution in reverse - with the top being our start/await point
        // middlewares.Reverse();
        // var next = initialExecute;
        //
        // foreach (var middleware in middlewares)
        //     next = () => (Task<TResult>)middlewareMethod.Invoke(middleware, [request, next, cancellationToken]);
        //
        // var result = await next().ConfigureAwait(false);
        // return result;
        var result = await initialExecute.Invoke().ConfigureAwait(false);
        return result;
    }
    
    
    static void AssertRequestHandlers(int count, object request)
    {
        if (count == 0)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        if (count > 1)
            throw new InvalidOperationException("More than 1 request handlers found for " + request.GetType().FullName);
    }
}