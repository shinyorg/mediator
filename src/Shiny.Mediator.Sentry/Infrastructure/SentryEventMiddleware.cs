namespace Shiny.Mediator.Infrastructure;

public class SentryEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    // would be nice to see a transaction across the event spray
    public async Task Process(
        EventContext<TEvent> context, 
        EventHandlerDelegate next, 
        CancellationToken cancellationToken
    )
    {
        var transaction = SentrySdk.StartTransaction("mediator", "event");
        var span = transaction.StartChild(context.Handler.GetType().FullName!);
        
        await next().ConfigureAwait(false);
        foreach (var header in context.Values)
            span.SetData(header.Key, header.Value);
        
        span.Finish();
        transaction.Finish();
    }
}