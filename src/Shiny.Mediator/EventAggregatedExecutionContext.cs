namespace Shiny.Mediator;

public class EventAggregatedExecutionContext<TEvent>(IReadOnlyList<EventContext<TEvent>> contexts) where TEvent : IEvent
{
    public IReadOnlyList<EventContext<TEvent>> HandlerExecutions => contexts;
}