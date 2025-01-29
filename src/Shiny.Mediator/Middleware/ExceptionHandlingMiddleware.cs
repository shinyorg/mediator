namespace Shiny.Mediator.Middleware;

public class ExceptionHandlingRequestMiddleware<TRequest, TResult>(
    IEnumerable<IExceptionHandler> handlers
) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        RequestContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
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
            var handled = false;
            foreach (var handler in handlers)
            {
                handled = await handler
                    .Handle(
                        context.Request!, 
                        context.Handler, 
                        ex
                    )
                    .ConfigureAwait(false);
                if (handled)
                    break;
            }

            if (!handled)
                throw;
        }
        return result;
    }
}

public class ExceptionHandlingCommandMiddleware<TCommand>(
    IEnumerable<IExceptionHandler> handlers
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(CommandContext<TCommand> context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
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
            var handled = false;
            foreach (var handler in handlers)
            {
                 handled = await handler
                     .Handle(
                         context.Command,
                         context.Handler,
                         ex
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

public class ExceptionHandlingEventMiddleware<TEvent>(
    IEnumerable<IExceptionHandler> handlers
) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public async Task Process(EventContext<TEvent> context, EventHandlerDelegate next, CancellationToken cancellationToken)
    {
        try
        {
            await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var handled = false;
            foreach (var handler in handlers)
            {
                handled = await handler
                    .Handle(
                        context.Event,
                        context.Handler,
                        ex
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