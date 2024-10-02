using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class ExceptionHandlerEventMiddleware<TEvent>(ILogger<TEvent> logger) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public async Task Process(
        EventExecutionContext<TEvent> context,
        EventHandlerDelegate next
    )
    {
        try
        {
            await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in event {EventType}", context.Event.GetType().FullName);
        }
    }
}