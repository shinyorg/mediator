namespace Shiny.Mediator;

public interface IEventCollector
{
    IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent;
}