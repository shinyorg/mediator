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
        IMediatorContext context,
        TCommand command,
        CancellationToken cancellationToken
    ) where TCommand : ICommand;
    
    /// <summary>
    /// Can send the command type
    /// </summary>
    /// <param name="command"></param> 
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    bool CanSend<TCommand>(TCommand command) where TCommand : ICommand;
}