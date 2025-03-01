namespace Shiny.Mediator.Infrastructure;


public interface ICommandExecutor
{
    /// <summary>
    /// Send a command
    /// </summary>
    /// <param name="context"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Send<TCommand>(
        MediatorContext context,
        TCommand command,
        CancellationToken cancellationToken
    ) where TCommand : ICommand;
}