using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class ExceptionHandlerEventMiddleware<TEvent>(ILogger<TEvent> logger) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public async Task Process(
        IEvent @event,
        EventHandlerDelegate next, 
        IEventHandler<TEvent> eventHandler, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in event {EventType}", @event.GetType().FullName);
        }
    }
}