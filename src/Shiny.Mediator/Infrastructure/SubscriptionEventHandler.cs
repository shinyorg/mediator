namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Delegate-backed <see cref="IEventHandler{TEvent}"/> registered against a
/// <see cref="SubscriptionEventCollector"/>. Returned by <see cref="IMediator.Subscribe{TEvent}"/> so the
/// caller can <c>Dispose</c> to remove the subscription.
/// </summary>
public class SubscriptionEventHandler<TEvent> : IDisposable, IEventHandler<TEvent> where TEvent : IEvent
{
    readonly SubscriptionEventCollector collector;


    /// <summary>
    /// Creates a subscription handler and registers it with <paramref name="collector"/>.
    /// </summary>
    public SubscriptionEventHandler(SubscriptionEventCollector collector)
    {
        this.collector = collector;
        this.collector.Add(this);
    }


    /// <summary>
    /// The delegate invoked for each published event. Must be set before the handler runs.
    /// </summary>
    public Func<TEvent, IMediatorContext, CancellationToken, Task>? OnHandle { get; set; }

    /// <inheritdoc/>
    public Task Handle(TEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        if (this.OnHandle == null)
            throw new InvalidOperationException("MediatorEventHandler.OnHandle is not set");

        return this.OnHandle.Invoke(@event, context, cancellationToken);
    }


    /// <summary>
    /// Removes the subscription from its collector.
    /// </summary>
    public void Dispose() => this.collector.Remove(this);
}