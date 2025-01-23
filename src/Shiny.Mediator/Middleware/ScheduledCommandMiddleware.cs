using Microsoft.Extensions.Logging;
using Shiny.Mediator.Services;

namespace Shiny.Mediator.Middleware;


public class ScheduledCommandMiddleware<TCommand>(
    ILogger<ScheduledCommandMiddleware<TCommand>> logger,
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : IScheduledCommand
{
    public async Task Process(
        CommandContext<TCommand> context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        if (context.Command.DueAt < DateTimeOffset.UtcNow)
        {
            logger.LogWarning($"Executing Scheduled Command '{context.Command}' that was due at {context.Command.DueAt}");
            await next().ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation($"Command '{context.Command}' scheduled for {context.Command.DueAt}");
            await scheduler
                .Schedule(context, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}