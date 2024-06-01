namespace Shiny.Mediator.Infrastructure;

public interface IEventPublisher
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default
    ) where TEvent : IEvent;
    
    // Task Publish(object arg, CancellationToken cancellationToken = default)

    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> action
    ) where TEvent : IEvent;
}