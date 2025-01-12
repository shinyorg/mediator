namespace Shiny.Mediator;


public interface IMediator
{
    /// <summary>
    /// This will send a request and return the context of the request with the result
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<ExecutionResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Send a `void` return request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default
    ) where TCommand : ICommand;
  
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    ExecutionResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default
    );
    
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