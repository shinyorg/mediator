namespace Shiny.Mediator;

/// <summary>
/// Handles published events of type <typeparamref name="TEvent"/>. Any number of handlers may be registered;
/// all are invoked when the event is published.
/// </summary>
public interface IEventHandler<TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Processes a published event after all event middleware has run.
    /// </summary>
    Task Handle(TEvent @event, IMediatorContext context, CancellationToken cancellationToken);
}
