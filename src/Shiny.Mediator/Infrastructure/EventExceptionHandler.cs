using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class EventExceptionHandler(ILogger<EventExceptionHandler> logger) : IExceptionHandler
{
    public Task<bool> Handle(object message, object handler, Exception exception, MediatorContext context)
    {
        var handled = false;
        if (message is IEvent)
        {
            logger.LogError(exception, "Error occurred in event handler");
            handled = true;
        }
        return Task.FromResult(handled);
    }
}