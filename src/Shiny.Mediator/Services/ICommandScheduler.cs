namespace Shiny.Mediator.Services;


public interface ICommandScheduler
{
    Task<bool> Schedule(IScheduledCommand command, CancellationToken cancellationToken);
}