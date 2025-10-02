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
        var transaction = ActivitySource.StartActivity("mediator", ActivityKind.Internal);
        var span = transaction != null ? ActivitySource.StartActivity(context.MessageHandler!.GetType().FullName!, ActivityKind.Internal, transaction.Context) : null;
        
        await next().ConfigureAwait(false);
        foreach (var header in context.Headers)
            span?.SetTag(header.Key, header.Value);
        
        span?.Dispose();
        transaction?.Dispose();
    }
}