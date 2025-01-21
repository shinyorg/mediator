namespace Shiny.Mediator;


public delegate Task CommandHandlerDelegate();
public interface ICommandMiddleware<TCommand> where TCommand : ICommand
{
    Task Process(
        CommandContext<TCommand> context,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    );
}