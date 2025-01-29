namespace Shiny.Mediator.Infrastructure;

public class GlobalExceptionHandler(IEnumerable<IExceptionHandler> handlers)
{
    public async Task<bool> Manage(object message, object handler, Exception exception)
    {
        var handled = false;
        foreach (var eh in handlers)
        {
            handled = await eh
                .Handle(
                    message,
                    handler,
                    exception
                )
                .ConfigureAwait(false);
                
            if (handled)
                break;
        }

        return handled;
    }   
}