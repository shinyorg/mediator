using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class ExceptionHandlingRequestMiddleware<TRequest, TResult>(
    GlobalExceptionHandler handler
) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        RequestContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
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
            var handled = await handler.Manage(context.Request!, context.Handler, ex).ConfigureAwait(false);
            if (!handled)
                throw;
        }
        return result;
    }
}

public class ExceptionHandlingCommandMiddleware<TCommand>(
    GlobalExceptionHandler handler
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(CommandContext<TCommand> context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (context.BypassExceptionHandlingEnabled())
            await next().ConfigureAwait(false);

        try
        {
            await next().ConfigureAwait(false);
        }
        catch (ValidateException)
        {
            throw; // this is a special case we let bubble through to prevent order of ops setup issues
        }
        catch (Exception ex)
        {
            var handled = await handler.Manage(context.Command!, context.Handler, ex).ConfigureAwait(false);
            if (!handled)
                throw;
        }
    }
}

public class ExceptionHandlingEventMiddleware<TEvent>(
    GlobalExceptionHandler handler
) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public async Task Process(EventContext<TEvent> context, EventHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (context.BypassExceptionHandlingEnabled())
            await next().ConfigureAwait(false);
        
        try
        {
            await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var handled = await handler.Manage(context.Event!, context.Handler, ex).ConfigureAwait(false);
            if (!handled)
                throw;
        }
    }
}

// public class ExceptionHandlingStreamRequestMiddleware<TRequest, TResult>(
//     GlobalExceptionHandler handler
// ) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
// {
//     public async IAsyncEnumerable<TResult> Process(
//         RequestContext<TRequest> context, 
//         StreamRequestHandlerDelegate<TResult> next,
//         CancellationToken cancellationToken
//     )
//     {
//         var handled = false;
//         IAsyncEnumerable<TResult> result = null!;
//         try
//         {
//             result = next();
//         }
//         catch (Exception ex)
//         {
//             handled = await handler.Manage(context.Request!, context.Handler, ex).ConfigureAwait(false);
//             if (!handled)
//                 throw;
//         }
//         return result;
//     }
// }