namespace Shiny.Mediator;

public interface ICommandHandler<TCommand>
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}