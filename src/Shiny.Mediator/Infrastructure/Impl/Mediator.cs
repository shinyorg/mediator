using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class Mediator(
    ILogger<Mediator> logger,
    IServiceProvider services,
    IRequestExecutor requestExecutor, 
    IStreamRequestExecutor streamRequestExecutor,
    ICommandExecutor commandExecutor, 
    IEventExecutor eventExecutor
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
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        var context = new MediatorContext(scope, request, activity, requestExecutor, commandExecutor, eventExecutor);
        configure?.Invoke(context);        
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
                logger.LogDebug("Event Returned by Request - Publishing: {EventType}", @event.GetType().FullName);
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
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        
        var context = new MediatorContext(scope, request, activity, requestExecutor, commandExecutor, eventExecutor);
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
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        
        var context = new MediatorContext(scope, command, activity, requestExecutor, commandExecutor, eventExecutor);
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
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        
        var context = new MediatorContext(scope, @event, activity, requestExecutor, commandExecutor, eventExecutor);
        configure?.Invoke(context);
        
        try
        {
            await eventExecutor
                .Publish(context, @event, executeInParallel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.Exception = exception;
            
            var handled = await this.TryHandle(context, exception).ConfigureAwait(false);
            if (!handled)
                throw;
        }
        return context;
    }


    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action) where TEvent : IEvent
        => eventExecutor.Subscribe(action);


    async Task<bool> TryHandle(MediatorContext context, Exception exception)
    {
        context.Exception = exception;
        
        if (context.BypassExceptionHandlingEnabled)
        {
            logger.LogDebug("Bypassing exception handling is enabled");
            return false;
        }

        var handled = false;
        using (context.StartActivity("Starting Exception Handling"))
        {
            var exceptionHandlers = context
                .ServiceScope
                .ServiceProvider
                .GetServices<IExceptionHandler>();
            
            foreach (var eh in exceptionHandlers)
            {
                var handlerType = eh.GetType().FullName ?? "Unknown";
                logger.LogDebug("Trying to handle exception with {HandlerType}", handlerType);
                
                handled = await eh
                    .Handle(
                        context,
                        exception
                    )
                    .ConfigureAwait(false);

                if (handled)
                {
                    logger.LogDebug(exception, "Exception handled by {HandlerType}", handlerType);
                    break;
                }
            }
        }

        if (!handled)
        {
            // we log as debug to let the exception bubble all the way out for the final app layers to decide the fate
            logger.LogDebug(exception, "No exception handlers managed the exception");
        }

        return handled;
    }
}