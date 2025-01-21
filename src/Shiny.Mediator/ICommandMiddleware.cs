namespace Shiny.Mediator;


public delegate Task CommandHandlerDelegate();
public interface ICommandMiddleware<TCommand> where TCommand : ICommand
{
    Task Process(
        TCommand command, 
        CommandContext context,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    );
}