namespace Shiny.Mediator;

/// <summary>
/// Delegate representing the next step in a request middleware chain. Invoke to continue execution and obtain the result.
/// </summary>
public delegate Task<TResult> RequestHandlerDelegate<TResult>();

/// <summary>
/// Middleware that wraps execution of an <see cref="IRequestHandler{TRequest,TResult}"/>. Multiple middleware
/// may be registered; their order is controlled by <see cref="MiddlewareOrderAttribute"/>.
/// </summary>
public interface IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    /// <summary>
    /// Wraps the request execution. Call <paramref name="next"/> to proceed to the next middleware or the handler.
    /// </summary>
    Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    );
}
