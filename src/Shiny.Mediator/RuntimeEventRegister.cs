using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public class RuntimeEventRegister : IEventCollector
{
    readonly List<object> handlers = new();
    
    public void Add<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
        => this.handlers.Add(handler);
    
    public void Remove<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
        => this.handlers.Remove(handler);
    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
        => this.handlers.OfType<IEventHandler<TEvent>>().ToList();
}