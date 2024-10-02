namespace Shiny.Mediator;

public class EventAggregatedExecutionContext<TEvent>(IReadOnlyList<EventExecutionContext<TEvent>> contexts) where TEvent : IEvent
{
    public IReadOnlyList<EventExecutionContext<TEvent>> HandlerExecutions => contexts;
}