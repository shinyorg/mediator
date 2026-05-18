namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Pluggable executor that runs a command and its middleware pipeline. The default in-process implementation
/// is <c>LocalCommandExecutor</c>; alternate executors can route certain command types elsewhere (e.g. RPC).
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Dispatches <paramref name="command"/> using <paramref name="context"/>'s service scope.
    /// </summary>
    Task Send<TCommand>(
        IMediatorContext context,
        TCommand command,
        CancellationToken cancellationToken
    ) where TCommand : ICommand;

    /// <summary>
    /// Returns true if this executor can handle <paramref name="command"/>. Used by <see cref="IMediatorDirector"/>
    /// to select an executor at dispatch time.
    /// </summary>
    bool CanSend<TCommand>(TCommand command) where TCommand : ICommand;
}
