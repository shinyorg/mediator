namespace Shiny.Mediator;


/// <summary>
/// Delegate representing the next step in a command middleware chain. Invoke to continue execution.
/// </summary>
public delegate Task CommandHandlerDelegate();

/// <summary>
/// Middleware that wraps execution of an <see cref="ICommandHandler{TCommand}"/>. Multiple middleware
/// may be registered for the same command type; their order is controlled by <see cref="MiddlewareOrderAttribute"/>.
/// </summary>
public interface ICommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Wraps the command execution. Call <paramref name="next"/> to proceed to the next middleware or the handler.
    /// </summary>
    Task Process(
        IMediatorContext context,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    );
}
