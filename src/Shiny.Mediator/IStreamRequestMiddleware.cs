namespace Shiny.Mediator;


/// <summary>
/// Delegate representing the next step in a stream request middleware chain. Invoke to obtain the result stream.
/// </summary>
public delegate IAsyncEnumerable<TResult> StreamRequestHandlerDelegate<TResult>();

/// <summary>
/// Middleware that wraps execution of an <see cref="IStreamRequestHandler{TRequest,TResult}"/>. Multiple middleware
/// may be registered; their order is controlled by <see cref="MiddlewareOrderAttribute"/>.
/// </summary>
public interface IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    /// <summary>
    /// Wraps the stream execution. Call <paramref name="next"/> to obtain the stream of results from the next middleware or handler.
    /// </summary>
    IAsyncEnumerable<TResult> Process(
        IMediatorContext context,
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    );
}
