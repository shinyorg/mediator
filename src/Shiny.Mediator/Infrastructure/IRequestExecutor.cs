namespace Shiny.Mediator.Infrastructure;


public interface IRequestExecutor
{
    /// <summary>
    /// This will send a request and return the context of the request with the result
    /// </summary>
    /// <param name="context"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<TResult> Request<TResult>(
        IMediatorContext context,
        IRequest<TResult> request,
        CancellationToken cancellationToken
    );
    
    
    /// <summary>
    /// Can handle the request type
    /// </summary>
    /// <param name="request"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    bool CanHandle<TResult>(IRequest<TResult> request);
}