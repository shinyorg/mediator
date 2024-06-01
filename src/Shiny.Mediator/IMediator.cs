namespace Shiny.Mediator;


public interface IMediator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Send<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest;


    // Task Send(object arg, CancellationToken cancellationToken = default)
    // Task<object?> Send(object arg, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<TResult> Send<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    );

    
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