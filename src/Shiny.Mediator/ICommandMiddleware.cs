namespace Shiny.Mediator;


public interface ICommandMiddleware<TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}