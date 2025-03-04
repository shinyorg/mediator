namespace Shiny.Mediator;


public interface IMediator
{
    /// <summary>
    /// Send a Command
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    Task<IMediatorContext> Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand;
    
    
    /// <summary>
    /// This will send a request and return the context of the request with the result
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    );
    
    
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    IAsyncEnumerable<TResult> Request<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    );
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task<IMediatorContext> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="action"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, IMediatorContext, CancellationToken, Task> action
    ) where TEvent : IEvent;
}