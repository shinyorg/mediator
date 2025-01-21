namespace Shiny.Mediator;

public class EventAggregatedContext<TEvent>(IReadOnlyList<EventContext<TEvent>> contexts) where TEvent : IEvent
{
    public IReadOnlyList<EventContext<TEvent>> HandlerExecutions => contexts;
}