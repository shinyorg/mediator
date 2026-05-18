namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Pluggable executor that runs a request and its middleware pipeline. The default in-process implementation
/// is <c>LocalRequestExecutor</c>; alternate executors can route certain request types elsewhere (e.g. RPC).
/// </summary>
public interface IRequestExecutor
{
    /// <summary>
    /// Dispatches <paramref name="request"/> using <paramref name="context"/>'s service scope and returns its result.
    /// </summary>
    Task<TResult> Request<TResult>(
        IMediatorContext context,
        IRequest<TResult> request,
        CancellationToken cancellationToken
    );


    /// <summary>
    /// Returns true if this executor can handle <paramref name="request"/>. Used by <see cref="IMediatorDirector"/>
    /// to select an executor at dispatch time.
    /// </summary>
    bool CanHandle<TResult>(IRequest<TResult> request);
}
