namespace Shiny.Mediator.Infrastructure;

public class GlobalExceptionHandler(IEnumerable<IExceptionHandler> handlers)
{
    public async Task<bool> Manage(object message, object handler, Exception exception, MediatorContext context)
    {
        var handled = false;
        foreach (var eh in handlers)
        {
            handled = await eh
                .Handle(
                    message,
                    handler,
                    exception,
                    context
                )
                .ConfigureAwait(false);
                
            if (handled)
                break;
        }

        return handled;
    }   
}