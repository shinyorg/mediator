namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Pluggable executor that fans an event out to its handlers. The default in-process implementation is
/// <c>LocalEventExecutor</c>; alternate executors can route certain event types elsewhere.
/// </summary>
public interface IEventExecutor
{
    /// <summary>
    /// Publishes <paramref name="event"/> to every applicable handler.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="event">The event to publish.</param>
    /// <param name="executeInParallel">When true, handlers run concurrently; when false, they run sequentially.</param>
    /// <param name="cancellationToken"></param>
    Task Publish<TEvent>(
        IMediatorContext context,
        TEvent @event,
        bool executeInParallel,
        CancellationToken cancellationToken
    ) where TEvent : IEvent;


    /// <summary>
    /// Subscribes a delegate-based handler for <typeparamref name="TEvent"/> in this executor's local scope.
    /// </summary>
    /// <returns>A disposable handle that removes the subscription when disposed.</returns>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, IMediatorContext, CancellationToken, Task> action
    ) where TEvent : IEvent;


    /// <summary>
    /// Returns true if this executor can publish events of <paramref name="eventType"/>. Used by
    /// <see cref="IMediatorDirector"/> to select an executor at publish time.
    /// </summary>
    bool CanPublish(Type eventType);
}
