namespace Shiny.Mediator.Services;


public interface ICommandScheduler
{
    /// <summary>
    /// Schedules and executes command
    /// </summary>
    /// <param name="context">The context containing the headers and contract</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> Schedule(
        CommandContext context,
        CancellationToken cancellationToken
    );
}