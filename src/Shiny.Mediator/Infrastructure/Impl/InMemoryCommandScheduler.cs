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
    readonly List<ScheduledItem> commands = new();
    ITimer? timer;


    /// <inheritdoc/>
    public Task Schedule(
        IMediatorContext context,
        DateTimeOffset dueAt,
        Func<IMediatorContext, CancellationToken, Task> dispatch,
        CancellationToken cancellationToken
    )
    {
        lock (this.commands)
        {
            this.commands.Add(new ScheduledItem(dueAt, context, dispatch));
            this.timer ??= timeProvider.CreateTimer(_ => this.OnTimerElapsed(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// Timer tick callback that scans pending commands and dispatches any whose
    /// <c>DueAt</c> has elapsed. Subclasses may override to customize scheduling behavior.
    /// </summary>
    protected virtual async void OnTimerElapsed()
    {
        this.timer!.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // stop while draining
        try
        {
            List<ScheduledItem> items;
            lock (this.commands)
                items = this.commands.ToList();

            foreach (var item in items)
            {
                if (item.DueAt >= timeProvider.GetUtcNow())
                    continue;

                var scope = services.CreateScope();
                var activity = MediatorActivitySource.Value.StartActivity();
                try
                {
                    item.Context.Rebuild(scope, activity);
                    item.Context.BypassMiddlewareEnabled = true;
                    item.Context.BypassExceptionHandlingEnabled = true;

                    await item.Dispatch(item.Context, CancellationToken.None).ConfigureAwait(false);
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
        finally
        {
            // always resume polling, even if draining threw before the per-item handler ran
            this.timer!.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
    }


    record ScheduledItem(
        DateTimeOffset DueAt,
        IMediatorContext Context,
        Func<IMediatorContext, CancellationToken, Task> Dispatch
    );
}
