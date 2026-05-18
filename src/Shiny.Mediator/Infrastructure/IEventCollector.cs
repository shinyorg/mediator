namespace Shiny.Mediator.Infrastructure;

/// <summary>
/// Supplies additional <see cref="IEventHandler{TEvent}"/> instances to be invoked at publish time alongside
/// the DI-registered handlers. Implementations can hold runtime-registered subscribers or remote handlers.
/// </summary>
public interface IEventCollector
{
    /// <summary>
    /// Returns the handlers known to this collector for <typeparamref name="TEvent"/>.
    /// </summary>
    IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent;


    // IDisposable OnHandlerDiscovered<TEvent>(Action<IEventHandler<TEvent>> onHandler) where TEvent : IEvent;
}
