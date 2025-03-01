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
        MediatorContext context,
        TEvent @event,
        bool executeInParallel,
        CancellationToken cancellationToken
    ) where TEvent : IEvent;
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, MediatorContext, CancellationToken, Task> action
    ) where TEvent : IEvent;
}