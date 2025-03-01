namespace Shiny.Mediator.Infrastructure;

public interface ICommandExecutor
{
    /// <summary>
    /// Send a `void` return request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    Task<MediatorContext> Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    ) where TCommand : ICommand;
}