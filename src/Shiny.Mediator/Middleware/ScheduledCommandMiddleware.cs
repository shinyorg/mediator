using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


[MiddlewareOrder(90)]
public class ScheduledCommandMiddleware<TCommand>(
    ILogger<ScheduledCommandMiddleware<TCommand>> logger,
    TimeProvider timeProvider,
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
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
                .Schedule(context, dueAt.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}