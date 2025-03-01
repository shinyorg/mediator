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
    RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        MediatorContext context,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken
    );
}