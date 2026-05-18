using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// In-memory <see cref="IEventCollector"/> that lets you register and remove <see cref="IEventHandler{TEvent}"/>
/// instances at runtime outside of dependency injection. Useful for ad-hoc, dynamic event handling.
/// </summary>
public class RuntimeEventRegister : IEventCollector
{
    readonly List<object> handlers = new();
    readonly Lock syncLock = new();

    /// <summary>
    /// Adds a handler that will receive events of <typeparamref name="TEvent"/> until removed.
    /// </summary>
    public void Add<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.syncLock)
        {
            this.handlers.Add(handler);
        }
    }

    /// <summary>
    /// Removes a previously added handler.
    /// </summary>
    public void Remove<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        lock (this.syncLock)
        {
            this.handlers.Remove(handler);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        lock (this.syncLock)
        {
            return this.handlers.OfType<IEventHandler<TEvent>>().ToList();
        }
    }
}
