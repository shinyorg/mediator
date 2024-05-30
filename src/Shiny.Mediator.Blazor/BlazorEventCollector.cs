namespace Shiny.Mediator.Blazor;

public class BlazorEventCollector : IEventCollector
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        // TODO: no idea yet
        throw new NotImplementedException();
    }
}