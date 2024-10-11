namespace Shiny.Mediator.Infrastructure;

public interface IEventPublisher
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task<EventAggregatedExecutionContext<TEvent>> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true
    ) where TEvent : IEvent;
    
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