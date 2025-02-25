namespace Shiny.Mediator.Infrastructure;


public interface IStreamRequestExecutor
{
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
}