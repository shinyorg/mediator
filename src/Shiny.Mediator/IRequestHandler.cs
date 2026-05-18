namespace Shiny.Mediator;

/// <summary>
/// Handles a request of type <typeparamref name="TRequest"/> and produces a <typeparamref name="TResult"/>.
/// Exactly one handler may be registered per request type.
/// </summary>
public interface IRequestHandler<TRequest, TResult> where TRequest : IRequest<TResult>
{
    /// <summary>
    /// Executes the request and returns its result. Called after all request middleware has run.
    /// </summary>
    Task<TResult> Handle(TRequest request, IMediatorContext context, CancellationToken cancellationToken);
}
