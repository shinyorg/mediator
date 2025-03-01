using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure;

public interface IRequestExecutor
{
    /// <summary>
    /// This will send a request and return the context of the request with the result
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<RequestResult<TResult>> RequestWithContext<TResult>(
        IServiceScope scope,
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    );
}