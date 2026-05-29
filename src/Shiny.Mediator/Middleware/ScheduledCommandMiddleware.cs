using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Defers command execution to a future time. Looks for a schedule on the context (set via
/// <c>SetCommandSchedule</c>) or on the command itself when it implements <see cref="IScheduledCommand"/>;
/// if the due time is in the future, hands the command off to the <see cref="ICommandScheduler"/> instead of
/// running it now. Registered by <c>AddCommandScheduling</c>.
/// </summary>
[MiddlewareOrder(90)]
public class ScheduledCommandMiddleware<TCommand>(
    ILogger<ScheduledCommandMiddleware<TCommand>> logger,
    TimeProvider timeProvider,
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
    public async Task Process(
        IMediatorContext context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var dueAt = context.TryGetCommandSchedule() ?? (context.Message as IScheduledCommand)?.DueAt;
        var now = timeProvider.GetUtcNow();
        
        if (dueAt == null || dueAt <= now)
        {
            logger.LogWarning(
                "Executing Scheduled Command '{message}' that was due at {dueAt}",
                context.Message,
                dueAt
            );
            await next().ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation("Command '{message}' scheduled for {dueAt}", context.Message, dueAt);
            await scheduler
                .Schedule(
                    context,
                    dueAt.Value,
                    // capture TCommand at compile time so the scheduler can fire without reflection
                    (ctx, ct) => ctx.Send((TCommand)ctx.Message, ct),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }
}