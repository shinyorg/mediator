using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class SubscriptionEventCollector : IEventCollector
{
    readonly List<object> handlers = new();
    

    public void Add<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.handlers)
            this.handlers.Add(handler);
    }

    
    public void Remove<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.handlers)
            this.handlers.Remove(handler);
    }


    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        lock (this.handlers)
            return this.handlers.OfType<IEventHandler<TEvent>>().ToList();
    }
}