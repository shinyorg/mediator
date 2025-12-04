namespace Shiny.Mediator.Infrastructure;


public interface IEventExecutor
{
    /// <summary>
    /// Publish an event
    /// </summary>
    /// <param name="context"></param>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task Publish<TEvent>(
        IMediatorContext context,
        TEvent @event,
        bool executeInParallel,
        CancellationToken cancellationToken
    ) where TEvent : IEvent;


    /// <summary>
    /// Publish an event to the background - this will also start a fresh service scope
    /// </summary>
    /// <param name="context"></param>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="onError"></param>
    /// <typeparam name="TEvent"></typeparam>
    void PublishToBackground<TEvent>(
        IMediatorContext context,
        TEvent @event,
        bool executeInParallel,
        Action<Exception> onError
    ) where TEvent : IEvent;
    
    /// <summary>
    /// Subscribe to an event
    /// </summary>
    /// <param name="action">The action to execute when the event is published</param>
    /// <typeparam name="TEvent">The event type to subscribe to</typeparam>
    /// <returns>A disposable to unsubscribe</returns>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, IMediatorContext, CancellationToken, Task> action
    ) where TEvent : IEvent;
    
    
    /// <summary>
    /// Can publish the event type
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    bool CanPublish(Type eventType);
}