namespace Shiny.Mediator;


/// <summary>
/// Handles a stream request of type <typeparamref name="TRequest"/>, producing an async sequence of <typeparamref name="TResult"/>.
/// Exactly one handler may be registered per stream request type.
/// </summary>
public interface IStreamRequestHandler<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    /// <summary>
    /// Executes the stream request and yields results over time. Called after all stream middleware has run.
    /// </summary>
    IAsyncEnumerable<TResult> Handle(
        TRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken
    );
}
