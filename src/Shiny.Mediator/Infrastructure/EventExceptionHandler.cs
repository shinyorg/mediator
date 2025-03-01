using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class EventExceptionHandler(ILogger<EventExceptionHandler> logger) : IExceptionHandler
{
    public Task<bool> Handle(MediatorContext context, Exception exception)
    {
        var handled = false;
        if (context.Message is IEvent)
        {
            logger.LogError(exception, "Error occurred in event handler");
            handled = true;
        }
        return Task.FromResult(handled);
    }
}