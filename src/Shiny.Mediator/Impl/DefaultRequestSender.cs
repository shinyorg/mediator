using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class DefaultRequestSender(IServiceProvider services) : IRequestSender
{
    public async Task Send(IRequest request, CancellationToken cancellationToken)
    {
        // using var scope = services.CreateScope();
        // var handlers = scope.ServiceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
        // AssertRequestHandlers(handlers.Count, request);
        //
        // await this.ExecuteMiddleware(
        //     scope, 
        //     (IRequest<Unit>)request, 
        //     async () =>
        //     {
        //         await handlers
        //             .First()
        //             .Handle(request, cancellationToken)
        //             .ConfigureAwait(false);
        //         return Unit.Value;
        //     },
        //     cancellationToken
        // )
        // .ConfigureAwait(false);
        throw new BadImageFormatException();
    }
    
    
    public async Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var task = (Task<TResult>)wrapperMethod.Invoke(wrapper, [services, request, cancellationToken])!;
        var result = await task.ConfigureAwait(false);
        return result;
    }
}


public class RequestWrapper<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Handle(IServiceProvider services, TRequest request, CancellationToken cancellationToken)
    {
        var handler = new RequestHandlerDelegate<TResult>(() => services
            .GetRequiredService<IRequestHandler<TRequest, TResult>>()!
            .Handle(request, cancellationToken)
        );
        
        var result = await services
            .GetServices<IRequestMiddleware<TRequest, TResult>>()
            .Reverse()
            .Aggregate(
                handler, 
                (next, middleware) => () => middleware.Process(
                    request, 
                    next, 
                    cancellationToken
                )
            )
            .Invoke()
            .ConfigureAwait(false);

        return result;
    }
}