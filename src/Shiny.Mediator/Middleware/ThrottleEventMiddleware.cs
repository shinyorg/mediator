using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class ThrottleEventMiddleware<TEvent>(
    ILogger<ThrottleEventMiddleware<TEvent>> logger,
    IConfiguration configuration
) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    record ThrottleState(long MillisecondsDelay) : IDisposable
    {
        readonly Lock syncLock = new();
        CancellationTokenSource? cts;
        EventHandlerDelegate? pendingNext;

        public void Enqueue(EventHandlerDelegate next, ILogger logger)
        {
            lock (this.syncLock)
            {
                this.cts?.Cancel();
                this.cts?.Dispose();
                this.cts = new CancellationTokenSource();
                this.pendingNext = next;

                var localCts = this.cts;
                _ = Task
                    .Delay(TimeSpan.FromMilliseconds(this.MillisecondsDelay), localCts.Token)
                    .ContinueWith(async t =>
                    {
                        if (t.IsCanceled)
                            return;

                        EventHandlerDelegate? toExecute;
                        lock (this.syncLock)
                        {
                            toExecute = this.pendingNext;
                            this.pendingNext = null;
                        }

                        if (toExecute != null)
                        {
                            try
                            {
                                await toExecute().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error executing throttled event handler");
                            }
                        }
                    }, TaskScheduler.Default);
            }
        }

        public void Dispose()
        {
            lock (this.syncLock)
            {
                this.cts?.Cancel();
                this.cts?.Dispose();
                this.cts = null;
                this.pendingNext = null;
            }
        }
    }

    readonly ConcurrentDictionary<string, ThrottleState> states = new();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue<long> is safe for trimming")]
    public Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var milliseconds = 0L;
        var attribute = context.GetHandlerAttribute<ThrottleAttribute>();
        if (attribute != null)
        {
            milliseconds = attribute.Milliseconds;
            logger.LogDebug("Throttle setting found on attribute");
        }

        if (milliseconds <= 0)
        {
            logger.LogDebug("Throttle will not be used - executing immediately");
            return next();
        }

        var handlerType = context.MessageHandler?.GetType().FullName ?? "unknown";
        var eventType = context.Message.GetType().FullName ?? "unknown";
        var key = $"{eventType}::{handlerType}";

        logger.LogDebug("Throttling event {EventType} for handler {HandlerType} with {Milliseconds}ms delay", eventType, handlerType, milliseconds);

        var state = this.states.GetOrAdd(key, _ => new ThrottleState(milliseconds));
        state.Enqueue(next, logger);

        return Task.CompletedTask;
    }
}