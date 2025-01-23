using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Shiny.Mediator.Services.Impl;


record RunStore(IScheduledCommand Command, string RunCallbackHeader);

public class InMemoryCommandScheduler : ICommandScheduler
{
    readonly List<RunStore> commands = new();
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
    
    
    public Task<bool> Schedule(string sendCallbackHeader, IScheduledCommand command, CancellationToken cancellationToken)
    {
        var scheduled = false;
        if (command.DueAt != null && command.DueAt < DateTimeOffset.UtcNow)
        {
            lock (this.commands)
                this.commands.Add(new(command, sendCallbackHeader));
            
            if (!this.timer.Enabled)
                this.timer.Start();
            
            scheduled = true;
        }
        return Task.FromResult(scheduled);
    }


    protected virtual async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        this.timer.Stop();
        List<RunStore> items = null!;
        lock (this.commands)
            items = this.commands.ToList();
        
        foreach (var item in items)
        {
            if (item.Command.DueAt >= DateTimeOffset.UtcNow)
            {
                try
                {
                    await this.mediator
                        .Send(item.Command, CancellationToken.None, (item.RunCallbackHeader, true))
                        .ConfigureAwait(false);
                    
                }
                catch (Exception ex)
                {
                    // might be picked up by other middleware
                    // TODO: retries?
                    this.logger.LogError(ex, "Error running scheduled command");
                }
                lock (this.commands)
                    this.commands.Remove(item);
            }
        }

        this.timer.Start();
    }
}