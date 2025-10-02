using System.Diagnostics;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryEventMiddleware<TEvent> : IEventMiddleware<TEvent> 
    where TEvent : IEvent
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator");
    
    // would be nice to see an activity across the event spray
    public async Task Process(
        IMediatorContext context, 
        EventHandlerDelegate next, 
        CancellationToken cancellationToken
    )
    {
        using var activity = ActivitySource.StartActivity("mediator.event", ActivityKind.Internal);
        activity?.SetTag("handler.type", context.MessageHandler!.GetType().FullName!);
        
        await next().ConfigureAwait(false);
        foreach (var header in context.Headers)
            activity?.SetTag(header.Key, header.Value);
    }
}