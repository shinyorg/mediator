using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure;

public interface ICommandExecutor
{
    /// <summary>
    /// Send a command
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    Task<MediatorContext> Send<TCommand>(
        IServiceScope scope,
        TCommand request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    ) where TCommand : ICommand;
}