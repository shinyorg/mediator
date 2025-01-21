using System.Collections.Concurrent;
using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Shiny.Mediator.Services;


public class InMemoryCommandScheduler : ICommandScheduler
{
    readonly ConcurrentDictionary<Guid, IScheduledCommand> commands = new();
    readonly ILogger logger;
    readonly IMediator mediator;
    readonly Timer timer = new();
    

    public InMemoryCommandScheduler(IMediator mediator, ILogger<ICommandScheduler> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
        this.timer.Interval = TimeSpan.FromSeconds(15).TotalMilliseconds;
        this.timer.Elapsed += this.OnTimerElapsed;
    }
    
    
    public Task<bool> Schedule(IScheduledCommand command, CancellationToken cancellationToken)
    {
        var scheduled = false;
        if (command.DueAt != null && command.DueAt < DateTimeOffset.UtcNow)
        {
            this.commands.Add(command);
            if (this.timer.Enabled)
                this.timer.Start();
            scheduled = true;
        }
        return Task.FromResult(scheduled);
    }


    protected virtual async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        this.timer.Stop();
        var items = this.commands.ToList();
        
        foreach (var item in items)
        {
            if (item.DueAt >= DateTimeOffset.UtcNow)
            {
                try
                {
                    // TODO: nullify due date so command can send
                    // item.DueAt = null;
                    await this.mediator
                        .Send(item)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // might be picked up by other middleware
                    // TODO: retries?
                    this.logger.LogError(ex, "Error running scheduled command");
                }
                // this.commands.Remove(item);
            }
        }

        this.timer.Start();
    }
}