namespace Shiny.Mediator.Infrastructure;


public interface IStreamRequestExecutor
{
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="context"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    IAsyncEnumerable<TResult> Request<TResult>(
        IMediatorContext context,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken
    );
    
    /// <summary>
    /// Can request the request type
    /// </summary>
    /// <param name="request"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    bool CanRequest<TResult>(IStreamRequest<TResult> request);
}