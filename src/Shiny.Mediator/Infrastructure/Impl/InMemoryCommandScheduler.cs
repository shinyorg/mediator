using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// In-memory <see cref="ICommandScheduler"/> that polls every minute and dispatches deferred commands when
/// their due time elapses. Scheduled commands do not survive process restarts.
/// </summary>
public class InMemoryCommandScheduler(
    ILogger<ICommandScheduler> logger,
    TimeProvider timeProvider,
    IServiceProvider services
) : ICommandScheduler
{
    readonly List<(DateTimeOffset DueAt, IMediatorContext Context)> commands = new();
    ITimer? timer;


    /// <inheritdoc/>
    public Task Schedule(IMediatorContext context, DateTimeOffset dueAt, CancellationToken cancellationToken)
    {
        lock (this.commands)
        {
            this.commands.Add((dueAt, context));
            this.timer ??= timeProvider.CreateTimer(_ => this.OnTimerElapsed(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        return Task.FromResult(true);
    }
    

    /// <summary>
    /// Timer tick callback that scans pending commands and dispatches any whose
    /// <c>DueAt</c> has elapsed. Subclasses may override to customize scheduling behavior.
    /// </summary>
    protected virtual async void OnTimerElapsed()
    {
        this.timer!.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // stop

        List<(DateTimeOffset DueAt, IMediatorContext Context)> items;
        lock (this.commands)
            items = this.commands.ToList();

        foreach (var item in items)
        {
            var time = timeProvider.GetUtcNow();
            if (item.DueAt < time)
            {
                var scope = services.CreateScope();
                var activity = MediatorActivitySource.Value.StartActivity();
                try
                {
                    lock (this.commands)
                        item.Context.Rebuild(scope, activity);

                    item.Context.BypassMiddlewareEnabled = true;
                    item.Context.BypassExceptionHandlingEnabled = true;

                    await item
                        .Context
                        .Send((ICommand)item.Context.Message, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error running scheduled command");
                }
                finally
                {
                    activity?.Dispose();
                    scope.Dispose();
                }
                lock (this.commands)
                    this.commands.Remove(item);
            }
        }

        // start again, but defer 1 min
        this.timer!.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
}