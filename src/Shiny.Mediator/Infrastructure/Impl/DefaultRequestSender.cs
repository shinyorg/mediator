using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure.Impl;


public class DefaultRequestSender(IServiceProvider services) : IRequestSender
{
    public async Task<ExecutionContext> Send(IRequest request, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        
        var wrapperType = typeof(RequestVoidWrapper<>).MakeGenericType([request.GetType()]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var task = (Task<ExecutionContext>)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        
        var context = await task.ConfigureAwait(false);
        return context;
    }


    public async Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var result = await this.RequestWithContext(request, cancellationToken).ConfigureAwait(false);
        return result.Result;
    }
    
    
    public async Task<ExecutionResult<TResult>> RequestWithContext<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        
        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var task = (Task<ExecutionResult<TResult>>)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        var result = await task.ConfigureAwait(false);
        
        return result;
    }
    
    
    public IAsyncEnumerable<TResult> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var context = this.RequestWithContext(request, cancellationToken);
        return context.Result;
    }
    
    
    public ExecutionResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        
        var wrapperType = typeof(StreamRequestWrapper<,>).MakeGenericType([request.GetType(), typeof(TResult)]);
        var wrapperMethod = wrapperType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)!;
        var wrapper = Activator.CreateInstance(wrapperType);
        var result = (ExecutionResult<IAsyncEnumerable<TResult>>)wrapperMethod.Invoke(wrapper, [scope.ServiceProvider, request, cancellationToken])!;
        
        return result;
    }
}