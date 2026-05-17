using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// <see cref="IEventCollector"/> that tracks <see cref="IEventHandler{TEvent}"/> instances added via
/// runtime subscriptions. Used internally by <c>LocalEventExecutor</c> to back delegate-style subscribers.
/// </summary>
public class SubscriptionEventCollector : IEventCollector
{
    readonly List<object> handlers = new();


    /// <summary>
    /// Registers a handler so it receives subsequent publications of <typeparamref name="TEvent"/>.
    /// </summary>
    public void Add<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.handlers)
            this.handlers.Add(handler);
    }


    /// <summary>
    /// Removes a previously added handler.
    /// </summary>
    public void Remove<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.handlers)
            this.handlers.Remove(handler);
    }


    /// <inheritdoc/>
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        lock (this.handlers)
            return this.handlers.OfType<IEventHandler<TEvent>>().ToList();
    }
}