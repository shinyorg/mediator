namespace Shiny.Mediator.Services;


public interface ICommandScheduler
{
    Task Schedule(IScheduledCommand command, CancellationToken cancellationToken);
}