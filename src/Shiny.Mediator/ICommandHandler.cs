namespace Shiny.Mediator;

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command, IMediatorContext context, CancellationToken cancellationToken);
}