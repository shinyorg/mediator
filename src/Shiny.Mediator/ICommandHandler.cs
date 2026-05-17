namespace Shiny.Mediator;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/>. Exactly one handler may be registered per command type.
/// </summary>
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Executes the command. Called by the mediator after all command middleware has run.
    /// </summary>
    Task Handle(TCommand command, IMediatorContext context, CancellationToken cancellationToken);
}
