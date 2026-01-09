using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public class RuntimeEventRegister : IEventCollector
{
    readonly List<object> handlers = new();
    readonly Lock syncLock = new();
    
    public void Add<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.syncLock)
        {
            this.handlers.Add(handler);
        }
    }
    
    public void Remove<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.syncLock)
        {
            this.handlers.Remove(handler);
        }
    }
    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        lock (this.syncLock)
        {
            return this.handlers.OfType<IEventHandler<TEvent>>().ToList();
        }
    }
}