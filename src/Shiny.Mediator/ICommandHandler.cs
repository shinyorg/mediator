namespace Shiny.Mediator;

public interface ICommandHandler;
public interface ICommandHandler<TCommand> : ICommandHandler where TCommand : ICommand
{
    Task Handle(TCommand command, CommandContext context, CancellationToken cancellationToken);
}