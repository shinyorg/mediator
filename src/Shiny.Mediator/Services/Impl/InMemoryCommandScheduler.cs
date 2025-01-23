using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Shiny.Mediator.Services.Impl;


public class InMemoryCommandScheduler : ICommandScheduler
{
    readonly List<CommandContext> commands = new();
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
    
    
    public Task<bool> Schedule(CommandContext command, CancellationToken cancellationToken)
    {
        var scheduled = false;
        if (command.Command is not IScheduledCommand scheduledCommand)
            throw new InvalidCastException($"Command {command.Command} is not of IScheduledCommand");
            
        if (scheduledCommand.DueAt < DateTimeOffset.UtcNow)
        {
            lock (this.commands)
                this.commands.Add(command);
            
            if (!this.timer.Enabled)
                this.timer.Start();
            
            scheduled = true;
        }
        return Task.FromResult(scheduled);
    }


    protected virtual async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        this.timer.Stop();
        List<CommandContext> items = null!;
        lock (this.commands)
            items = this.commands.ToList();
        
        foreach (var item in items)
        {
            var command = (IScheduledCommand)item.Command;
            if (command.DueAt >= DateTimeOffset.UtcNow)
            {
                var headers = item
                    .Values
                    .Select(x => (Key: x.Key, Value: x.Value))
                    .ToList();
                
                try
                {
                    await this.mediator
                        .Send(command, CancellationToken.None, headers)
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