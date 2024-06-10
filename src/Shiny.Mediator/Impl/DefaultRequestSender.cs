using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class DefaultRequestSender(IServiceProvider services) : IRequestSender
{
    public async Task Send(IRequest request, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType([request.GetType(), typeof(Unit)]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var task = (Task<Unit>)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        await task.ConfigureAwait(false);
    }
    
    
    public async Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var task = (Task<TResult>)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        var result = await task.ConfigureAwait(false);
        return result;
    }
}


class RequestWrapper<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var requestHandler = services.GetService<IRequestHandler<TRequest, TResult>>();
        if (requestHandler == null)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);
        
        var handler = new RequestHandlerDelegate<TResult>(()
            => requestHandler.Handle(request, cancellationToken));
        
        var result = await services
            .GetServices<IRequestMiddleware<TRequest, TResult>>()
            .Reverse()
            .Aggregate(
                handler, 
                (next, middleware) => () => middleware.Process(
                    request, 
                    next, 
                    requestHandler,
                    cancellationToken
                )
            )
            .Invoke()
            .ConfigureAwait(false);

        return result;
    }
}