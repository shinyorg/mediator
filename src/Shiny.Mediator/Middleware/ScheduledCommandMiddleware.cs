using Microsoft.Extensions.Logging;
using Shiny.Mediator.Services;

namespace Shiny.Mediator.Middleware;

public class ScheduledCommandMiddleware<TCommand>(
    ILogger<ScheduledCommandMiddleware<TCommand>> logger,
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : IScheduledCommand
{
    const string CB_HEADER = "RunNow";
    
    
    public async Task Process(
        CommandContext<TCommand> context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        if (context.Command.DueAt < DateTimeOffset.UtcNow || context.Values.ContainsKey(CB_HEADER))
        {
            logger.LogWarning($"Executing Scheduled Command that was due at {context.Command.DueAt}");
            await next().ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation($"Command scheduled for {context.Command.DueAt}");
            await scheduler
                .Schedule(CB_HEADER, context.Command, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}