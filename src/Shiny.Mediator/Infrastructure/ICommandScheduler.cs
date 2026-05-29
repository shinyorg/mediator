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
    /// <param name="dispatch">
    /// A typed closure provided by the scheduling middleware that invokes the command through the mediator using its
    /// compile-time type. In-process schedulers should call this to fire the command. Schedulers that persist commands
    /// across process restarts (the closure cannot survive a restart) can ignore this and rehydrate the command themselves.
    /// </param>
    /// <param name="cancellationToken"></param>
    Task Schedule(
        IMediatorContext context,
        DateTimeOffset dueAt,
        Func<IMediatorContext, CancellationToken, Task> dispatch,
        CancellationToken cancellationToken
    );
}
