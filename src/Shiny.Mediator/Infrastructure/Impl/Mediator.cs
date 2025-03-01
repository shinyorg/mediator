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
    

    public async Task<RequestResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    )
    {
        var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, activitySource, headers);
        using var activity = context.StartActivity("Request");
        
        var execution = await requestExecutor
            .RequestWithContext(
                context,
                request, 
                cancellationToken 
            )
            .ConfigureAwait(false);

        if (execution.Result is IEvent @event)
        {
            // TODO: publish child telemetry here
            var child = context.CreateChild(@event);
            await eventExecutor
                .Publish(child, @event, true, cancellationToken)
                .ConfigureAwait(false);
        }

        return execution;
    }


    public RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    )
    {
        // we create the scope here, but we do not dispose of it
        var context = new MediatorContext(services.CreateScope(), request, activitySource, headers); 
        return streamRequestExecutor.RequestWithContext(context, request, cancellationToken);
    }


    public async Task<MediatorContext> Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    ) where TCommand : ICommand
    {
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, request, activitySource, headers);
        await commandExecutor.Send(context, request, cancellationToken).ConfigureAwait(false);

        return context;
    }


    public async Task<MediatorContext> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        params IEnumerable<(string Key, object Value)> headers
    ) where TEvent : IEvent
    {
        // TODO: IExceptionHandlers move here
        using var scope = services.CreateScope();
        var context = new MediatorContext(scope, @event, activitySource, headers);
        await eventExecutor.Publish(context, @event, executeInParallel, cancellationToken);
        return context;
    }


    public IDisposable Subscribe<TEvent>(Func<TEvent, MediatorContext, CancellationToken, Task> action) where TEvent : IEvent
        => eventExecutor.Subscribe(action);


    async Task ExecuteSafe(MediatorContext context, Func<Task> task)
    {
        if (context.BypassExceptionHandlingEnabled())
            await task().ConfigureAwait(false);
       
        try
        {
            await task().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
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

            if (!handled)
                throw;
        }
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


/*
 // get scope creation and top level exception handler up here, not in the executors
if (context.BypassExceptionHandlingEnabled())
       await next().ConfigureAwait(false);

   TResult result = default;
   try
   {
       result = await next().ConfigureAwait(false);
   }
   catch (ValidateException)
   {
       throw; // this is a special case we let bubble through to prevent order of ops setup issues
   }
   catch (Exception ex)
   {
       var handled = await handler
           .Manage(context.Message!, context.MessageHandler, ex, context)
           .ConfigureAwait(false);
       
       if (!handled)
           throw;
   }
   return result;
 */