namespace Shiny.Mediator;


public interface IMediator
{
    /// <summary>
    /// Send a Command
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    Task<MediatorContext> Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    ) where TCommand : ICommand;
    
    
    /// <summary>
    /// This will send a request and return the context of the request with the result
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<RequestResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    );
    
    
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    );
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task<EventAggregatedContext> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        params IEnumerable<(string Key, object Value)> headers
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