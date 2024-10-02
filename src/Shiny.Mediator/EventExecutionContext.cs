namespace Shiny.Mediator;


public class EventExecutionContext<TEvent>(
    TEvent @event,
    IEventHandler<TEvent> eventHandler,
    CancellationToken cancellationToken
) where TEvent : IEvent
{
    public TEvent Event => @event;
    public IEventHandler<TEvent> EventHandler => eventHandler;
    public CancellationToken CancellationToken => cancellationToken;
}
public class EventAggregatedExecutionContext<TEvent>(IReadOnlyList<EventExecutionContext<TEvent>> contexts)
    where TEvent : IEvent
{
    
}