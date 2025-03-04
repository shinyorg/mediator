using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class InMemoryCommandScheduler(
    ILogger<ICommandScheduler> logger,
    TimeProvider timeProvider,
    IServiceProvider services
) : ICommandScheduler
{
    readonly List<(DateTimeOffset DueAt, IMediatorContext Context)> commands = new();
    ITimer? timer;
    
    
    public Task<bool> Schedule(IMediatorContext command, DateTimeOffset dueAt, CancellationToken cancellationToken)
    {
        var scheduled = false;
        var now = timeProvider.GetUtcNow();
        
        if (dueAt > now)
        {
            lock (this.commands)
                this.commands.Add((dueAt, command));

            scheduled = true;
            this.timer ??= timeProvider.CreateTimer(_ => this.OnTimerElapsed(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        return Task.FromResult(scheduled);
    }
    

    protected virtual async void OnTimerElapsed()
    {
        this.timer!.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // stop
        
        List<(DateTimeOffset DueAt, IMediatorContext Context)> items = null!;
        lock (this.commands)
            items = this.commands.ToList();
        
        foreach (var item in items)
        {
            var time = timeProvider.GetUtcNow();
            if (item.DueAt < time)
            {
                try
                {
                    using var scope = services.CreateScope();
                    item.Context.Rebuild(scope);
                    await item
                        .Context
                        .Send((ICommand)item.Context.Message, CancellationToken.None)
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

        // start again, but defer 1 min
        this.timer!.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
}