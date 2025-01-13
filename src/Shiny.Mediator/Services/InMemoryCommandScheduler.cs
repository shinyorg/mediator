namespace Shiny.Mediator.Services;


public class InMemoryCommandScheduler : ICommandScheduler
{
    public Task Schedule(IScheduledCommand command, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}