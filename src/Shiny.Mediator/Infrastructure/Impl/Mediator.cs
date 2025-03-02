using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure.Impl;


// technically, I want the context to be used in handler within handler calls... we can worry about that in a future release
// mediator could be scoped but MAUI doesn't deal well with that
    // scoped would allow me to inject mediatorcontext which would stay with scope
public class Mediator(
    IServiceProvider services,
    IRequestExecutor requestExecutor, 
    IStreamRequestExecutor streamRequestExecutor,
    ICommandExecutor commandExecutor, 
    IEventExecutor eventExecutor,
    IEnumerable<IExceptionHandler> exceptionHandlers
) : IMediator
{
    static readonly ActivitySource activitySource = new("Shiny.Mediator");
    

    public async Task<TResult> Request<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, activitySource);
        configure?.Invoke(context);
        using var activity = context.StartActivity("Request");
        
        TResult result = default!;
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
        return result;
    }


    public async IAsyncEnumerable<TResult> Request<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, activitySource);
        configure?.Invoke(context);
        var enumerable = streamRequestExecutor.Request(context, request, cancellationToken);

        await foreach (var result in enumerable)
        {
            yield return result;
        }
    }


    public async Task<IMediatorContext> Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, activitySource);
        configure?.Invoke(context);
        
        try
        {
            await commandExecutor.Send(context, request, cancellationToken).ConfigureAwait(false);
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
        var context = new MediatorContext(scope, @event, activitySource);
        configure?.Invoke(context);
        
        try
        {
            await eventExecutor.Publish(context, @event, executeInParallel, cancellationToken);
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
// public async Task<MediatorResult> Request<TResult>(
//     IRequest<TResult> request,
//     CancellationToken cancellationToken = default,
//     params IEnumerable<(string Key, object Value)> headers
// )
// {
//     try
//     {
//         var context = await this
//             .RequestWithContext(request, cancellationToken, headers)
//             .ConfigureAwait(false);
//
//         // TODO: this gets me nothing that the context didn't already have... however, I'm returning a loose object, so I can transform
//         // the result now
//         return new MediatorResult(
//             request,
//             context.Result,
//             null,
//             context.Context
//         );
//     }
//     catch (Exception ex)
//     {
//         // TODO: could apply different exception handler allowing Result to set/handled
//         return new MediatorResult(
//             request,
//             null,
//             ex,
//             null // TODO: context is lost and shouldn't be on exceptions
//         );   
//     }
// }
//
// public record MediatorResult(
//     object Contract,
//     object? Result,
//     Exception? Exception,
//     IMediatorContext Context
// );