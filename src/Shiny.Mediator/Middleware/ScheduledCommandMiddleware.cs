using Shiny.Mediator.Services;

namespace Shiny.Mediator.Middleware;

public class ScheduledCommandMiddleware<TCommand>(
    ICommandScheduler scheduler
) : ICommandMiddleware<TCommand> where TCommand : IScheduledCommand
{
    public async Task Process(TCommand command, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (command.DueAt == null)
        {
            await next().ConfigureAwait(false);
        }
        else
        {
            await scheduler.Schedule(command, cancellationToken).ConfigureAwait(false);
        }
    }
}