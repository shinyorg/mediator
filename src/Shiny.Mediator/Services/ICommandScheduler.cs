namespace Shiny.Mediator.Services;


public interface ICommandScheduler
{
    /// <summary>
    /// Schedules and executes command
    /// </summary>
    /// <param name="sendCallbackHeader">Send this in headers back to mediator for middleware to execute and not reschedule</param>
    /// <param name="command">The command to store</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> Schedule(
        string sendCallbackHeader,
        IScheduledCommand command, 
        CancellationToken cancellationToken
    );
}