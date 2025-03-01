namespace Shiny.Mediator;


public delegate Task CommandHandlerDelegate();
public interface ICommandMiddleware<TCommand> where TCommand : ICommand
{
    Task Process(
        MediatorContext context,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    );
}