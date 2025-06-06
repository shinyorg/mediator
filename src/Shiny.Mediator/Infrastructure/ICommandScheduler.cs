namespace Shiny.Mediator.Infrastructure;


public interface ICommandScheduler
{
    /// <summary>
    /// Schedules and executes command
    /// </summary>
    /// <param name="context">The context containing the headers and contract</param>
    /// <param name="dueAt">The schedule date</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Schedule(
        IMediatorContext context,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken
    );
}