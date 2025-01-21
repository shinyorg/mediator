using System.Collections.Concurrent;
using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Shiny.Mediator.Services.Impl;


public class InMemoryCommandScheduler : ICommandScheduler
{
    readonly ConcurrentDictionary<Guid, CommandContext> commands = new();
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
    
    
    public Task<bool> Schedule(CommandContext context, CancellationToken cancellationToken)
    {
        if (context.Command is not IScheduledCommand command)
            return Task.FromResult(false);
        
        var scheduled = false;
        if (command.DueAt != null && command.DueAt < DateTimeOffset.UtcNow)
        {
            this.commands.TryAdd(context.Id, context);
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
            var scheduled = (IScheduledCommand)item.Value.Command;
            
            if (scheduled.DueAt >= DateTimeOffset.UtcNow)
            {
                try
                {
                    scheduled.DueAt = null;
                    await this.mediator
                        .Send(scheduled)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // might be picked up by other middleware
                    // TODO: retries?
                    this.logger.LogError(ex, "Error running scheduled command");
                }
                this.commands.TryRemove(item.Key, out _);
            }
        }

        this.timer.Start();
    }
}