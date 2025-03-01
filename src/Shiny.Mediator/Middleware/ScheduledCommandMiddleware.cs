using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class ScheduledCommandMiddleware<TCommand>(
    ILogger<ScheduledCommandMiddleware<TCommand>> logger,
    TimeProvider timeProvider,
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(
        MediatorContext context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var dueAt = context.TryGetCommandSchedule() ?? (context.Message as IScheduledCommand)?.DueAt;
        var now = timeProvider.GetUtcNow();
        if (dueAt == null || dueAt < now)
        {
            logger.LogWarning($"Executing Scheduled Command '{context.Message}' that was due at {dueAt}");
            await next().ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation($"Command '{context.Message}' scheduled for {dueAt}");
            await scheduler
                .Schedule(context, dueAt.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}