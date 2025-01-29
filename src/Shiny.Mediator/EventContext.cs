namespace Shiny.Mediator;


public class EventContext : AbstractMediatorContext;

public class EventContext<TEvent>(
    TEvent @event,
    IEventHandler<TEvent> eventHandler
) : EventContext where TEvent : IEvent
{
    public TEvent Event => @event;
    public IEventHandler<TEvent> EventHandler => eventHandler;
}