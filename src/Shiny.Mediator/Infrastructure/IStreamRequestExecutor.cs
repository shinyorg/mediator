using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure;


public interface IStreamRequestExecutor
{
    /// <summary>
    /// Requests a stream of data from a message
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        IServiceScope scope,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    );
}