using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


[MiddlewareOrder(50)]
public class SampleEventMiddleware<TEvent>(
    ILogger<SampleEventMiddleware<TEvent>> logger,
    IConfiguration configuration
) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    record SampleState(long MillisecondsDelay) : IDisposable
    {
        readonly Lock syncLock = new();
        bool timerRunning;
        EventHandlerDelegate? pendingNext;

        public void Enqueue(EventHandlerDelegate next, ILogger logger)
        {
            lock (this.syncLock)
            {
                this.pendingNext = next;

                if (this.timerRunning)
                    return;

                this.timerRunning = true;
                _ = Task
                    .Delay(TimeSpan.FromMilliseconds(this.MillisecondsDelay))
                    .ContinueWith(async _ =>
                    {
                        EventHandlerDelegate? toExecute;
                        lock (this.syncLock)
                        {
                            toExecute = this.pendingNext;
                            this.pendingNext = null;
                            this.timerRunning = false;
                        }

                        if (toExecute != null)
                        {
                            try
                            {
                                await toExecute().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error executing sampled event handler");
                            }
                        }
                    }, TaskScheduler.Default);
            }
        }

        public void Dispose()
        {
            lock (this.syncLock)
            {
                this.pendingNext = null;
                this.timerRunning = false;
            }
        }
    }

    readonly ConcurrentDictionary<string, SampleState> states = new();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue<long> is safe for trimming")]
    public Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var milliseconds = 0L;
        var attribute = context.GetHandlerAttribute<SampleAttribute>();
        if (attribute != null)
        {
            milliseconds = attribute.Milliseconds;
            logger.LogDebug("Sample setting found on attribute");
        }

        if (milliseconds <= 0)
        {
            logger.LogDebug("Sample will not be used - executing immediately");
            return next();
        }

        var handlerType = context.MessageHandler?.GetType().FullName ?? "unknown";
        var eventType = context.Message.GetType().FullName ?? "unknown";
        var key = $"{eventType}::{handlerType}";

        logger.LogDebug("Sampling event {EventType} for handler {HandlerType} with {Milliseconds}ms window", eventType, handlerType, milliseconds);

        var state = this.states.GetOrAdd(key, _ => new SampleState(milliseconds));
        state.Enqueue(next, logger);

        return Task.CompletedTask;
    }
}
