namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Pluggable executor that runs a stream request and its middleware pipeline. The default in-process
/// implementation is <c>LocalStreamRequestExecutor</c>; alternate executors can route certain stream
/// request types elsewhere (e.g. RPC).
/// </summary>
public interface IStreamRequestExecutor
{
    /// <summary>
    /// Dispatches <paramref name="request"/> using <paramref name="context"/>'s service scope and returns
    /// the async sequence of results.
    /// </summary>
    IAsyncEnumerable<TResult> Request<TResult>(
        IMediatorContext context,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Returns true if this executor can handle <paramref name="request"/>. Used by <see cref="IMediatorDirector"/>
    /// to select an executor at dispatch time.
    /// </summary>
    bool CanRequest<TResult>(IStreamRequest<TResult> request);
}
