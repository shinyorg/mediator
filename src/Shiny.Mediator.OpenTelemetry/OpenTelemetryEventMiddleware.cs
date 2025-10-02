using System.Diagnostics;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryEventMiddleware<TEvent> : IEventMiddleware<TEvent> 
    where TEvent : IEvent
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator", "1.0.0");

    public async Task Process(
        IMediatorContext context, 
        EventHandlerDelegate next, 
        CancellationToken cancellationToken
    )
    {
        using var activity = ActivitySource.StartActivity("mediator.event", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("handler.type", context.MessageHandler?.GetType().FullName);
            activity.SetTag("event.type", typeof(TEvent).FullName);
            
            foreach (var header in context.Headers)
            {
                activity.SetTag($"context.header.{header.Key}", header.Value);
            }
        }

        try
        {
            await next().ConfigureAwait(false);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}