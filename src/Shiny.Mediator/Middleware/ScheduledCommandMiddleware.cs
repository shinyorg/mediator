using Shiny.Mediator.Services;

namespace Shiny.Mediator.Middleware;

public class ScheduledCommandMiddleware<TCommand>(
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : IScheduledCommand
{
    public async Task Process(
        CommandContext<TCommand> context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        if (context.Command.DueAt == null)
        {
            await next().ConfigureAwait(false);
        }
        else
        {
            await scheduler
                .Schedule(context, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}