namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Backend used by <c>ScheduledCommandMiddleware</c> to defer execution of an <see cref="IScheduledCommand"/>
/// until its due time. Implementations decide where the schedule is stored (in-memory, persisted, distributed).
/// </summary>
public interface ICommandScheduler
{
    /// <summary>
    /// Persists the command described by <paramref name="context"/> and arranges for it to run at <paramref name="dueAt"/>.
    /// </summary>
    /// <param name="context">The context whose <c>Message</c> is the command to defer.</param>
    /// <param name="dueAt">UTC time at which the command should execute.</param>
    /// <param name="cancellationToken"></param>
    Task Schedule(
        IMediatorContext context,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken
    );
}
