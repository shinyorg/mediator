using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure.Impl;


public class Mediator(
    IServiceProvider services,
    IRequestExecutor requestExecutor, 
    IStreamRequestExecutor streamRequestExecutor,
    ICommandExecutor commandExecutor, 
    IEventExecutor eventExecutor,
    IEnumerable<IExceptionHandler> exceptionHandlers
) : IMediator
{
    public async Task<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        TResult result = default!;
        
        var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, requestExecutor, commandExecutor, eventExecutor);
        configure?.Invoke(context);
        using var activity = context.StartActivity("Request");
        
        try
        {
            result = await requestExecutor
                .Request(
                    context,
                    request,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result is IEvent @event)
            {
                var child = context.CreateChild(@event);
                await eventExecutor
                    .Publish(child, @event, true, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            var handled = await this
                .TryHandle(context, exception)
                .ConfigureAwait(false);

            if (!handled)
                throw;
        }
        return (context, result);
    }


    public async IAsyncEnumerable<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IStreamRequest<TResult> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, requestExecutor, commandExecutor, eventExecutor);
        configure?.Invoke(context);
        var enumerable = streamRequestExecutor.Request(context, request, cancellationToken);

        await foreach (var result in enumerable)
        {
            yield return (context, result);
        }
    }


    public async Task<IMediatorContext> Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, command, requestExecutor, commandExecutor, eventExecutor);
        configure?.Invoke(context);
        
        try
        {
            await commandExecutor
                .Send(context, command, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var handled = await this
                .TryHandle(context, exception)
                .ConfigureAwait(false);
            
            if (!handled)
                throw;
        }

        return context;
    }


    public async Task<IMediatorContext> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, @event, requestExecutor, commandExecutor, eventExecutor);
        configure?.Invoke(context);
        
        try
        {
            await eventExecutor
                .Publish(context, @event, executeInParallel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var handled = await this.TryHandle(context, exception).ConfigureAwait(false);
            if (!handled)
                throw;
        }
        return context;
    }


    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action) where TEvent : IEvent
        => eventExecutor.Subscribe(action);


    async Task<bool> TryHandle(IMediatorContext context, Exception exception)
    {
        if (context.BypassExceptionHandlingEnabled)
            return false;
            
        var handled = false;
        foreach (var eh in exceptionHandlers)
        {
            handled = await eh
                .Handle(
                    context,
                    exception
                )
                .ConfigureAwait(false);

            if (handled)
                break;
        }

        return handled;
    }
}