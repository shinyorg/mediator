namespace Shiny.Mediator.Infrastructure;

public interface IRequestSender
{
    // Task Send(object arg, CancellationToken cancellationToken = default)
    // Task<object?> Send(object arg, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<TResult> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    );
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Send(
        IRequest request,
        CancellationToken cancellationToken = default
    );


    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    IAsyncEnumerable<TResult> Request<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default
    );
}