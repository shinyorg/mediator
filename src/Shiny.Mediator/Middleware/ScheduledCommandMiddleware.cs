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
        CommandContext<TCommand> context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var dueAt = context.TryGetCommandSchedule() ?? (context.Command as IScheduledCommand)?.DueAt;
        var now = timeProvider.GetUtcNow();
        if (dueAt == null || dueAt < now)
        {
            logger.LogWarning($"Executing Scheduled Command '{context.Command}' that was due at {context.Command.DueAt}");
            await next().ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation($"Command '{context.Command}' scheduled for {dueAt}");
            await scheduler
                .Schedule(context, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}