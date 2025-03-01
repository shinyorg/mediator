namespace Shiny.Mediator;

public class EventAggregatedContext(IReadOnlyList<MediatorContext> contexts)
{
    public IReadOnlyList<MediatorContext> HandlerExecutions => contexts;
}