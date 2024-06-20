using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class DefaultRequestSender(IServiceProvider services) : IRequestSender
{
    public async Task Send(IRequest request, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        
        var wrapperType = typeof(RequestVoidWrapper<>).MakeGenericType([request.GetType()]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var task = (Task)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        
        await task.ConfigureAwait(false);
    }
    

    public IAsyncEnumerable<TResult> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        
        var wrapperType = typeof(StreamRequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var enumerable = (IAsyncEnumerable<TResult>)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        
        return enumerable;
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