namespace Shiny.Mediator.Services;


public interface ICommandScheduler
{
    Task<bool> Schedule(CommandContext context, CancellationToken cancellationToken);
}