namespace Shiny.Mediator;


public interface IMediator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <returns></returns>
    Task Send<TRequest>(
        TRequest command, 
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest;

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<TResult> Send<TRequest, TResult>(
        TRequest command, 
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest<TResult>;

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="fireAndForget"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task Publish<TEvent>(
        TEvent @event, 
        bool fireAndForget = true, 
        bool executeInParallel = true, 
        CancellationToken cancellationToken = default
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