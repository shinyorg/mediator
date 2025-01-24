using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class InMemoryCommandScheduler(
    IMediator mediator, 
    ILogger<ICommandScheduler> logger,
    TimeProvider timeProvider
) : ICommandScheduler
{
    readonly List<CommandContext> commands = new();
    ITimer? timer;
        
    
    
    public Task<bool> Schedule(CommandContext command, CancellationToken cancellationToken)
    {
        var scheduled = false;
        if (command.Command is not IScheduledCommand scheduledCommand)
            throw new InvalidCastException($"Command {command.Command} is not of IScheduledCommand");
            
        if (scheduledCommand.DueAt < DateTimeOffset.UtcNow)
        {
            lock (this.commands)
                this.commands.Add(command);

            scheduled = true;
            this.timer ??= timeProvider.CreateTimer(_ => this.OnTimerElapsed(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        return Task.FromResult(scheduled);
    }
    

    protected virtual async void OnTimerElapsed()
    {
        this.timer!.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // stop
        
        List<CommandContext> items = null!;
        lock (this.commands)
            items = this.commands.ToList();
        
        foreach (var item in items)
        {
            var command = (IScheduledCommand)item.Command;
            if (command.DueAt < timeProvider.GetUtcNow())
            {
                var headers = item
                    .Values
                    .Select(x => (Key: x.Key, Value: x.Value))
                    .ToList();
                
                try
                {
                    await mediator
                        .Send(command, CancellationToken.None, headers)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // might be picked up by other middleware
                    // TODO: retries?
                    logger.LogError(ex, "Error running scheduled command");
                }
                lock (this.commands)
                    this.commands.Remove(item);
            }
        }

        this.timer!.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
}