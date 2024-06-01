using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class DefaultRequestSender(IServiceProvider services) : IRequestSender
{
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        // TODO: middleware execution should support contravariance
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        // TODO: pipelines
        await handlers
            .First()
            .Handle(request, cancellationToken)
            .ConfigureAwait(false);
    }
    
    
    public async Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));

        // TODO: middleware execution should support contravariance
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        var handler = handlers.First();
        var handleMethod = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public)!;
        var resultTask = (Task<TResult>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        var result = await resultTask.ConfigureAwait(false);
        
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