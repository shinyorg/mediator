using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


// TODO: this won't execute on the mainthread
/// <summary>
/// Event middleware that applies sampling (<see cref="SampleAttribute"/>) and throttling
/// (<see cref="ThrottleAttribute"/>) policies on a per handler-method basis. Sampling collects the last event
/// within a window and runs it when the timer fires; throttling runs the first event immediately and discards
/// subsequent ones until the cooldown expires.
/// </summary>
[MiddlewareOrder(50)]
public class QueuedEventMiddleware<TEvent>(
    ILogger<QueuedEventMiddleware<TEvent>> logger
) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    record SampleState(long MillisecondsDelay) : IDisposable
    {
        readonly Lock syncLock = new();
        bool timerRunning;
        CancellationTokenSource? timerCts;
        EventHandlerDelegate? pendingNext;

        public void Enqueue(EventHandlerDelegate next, ILogger logger)
        {
            lock (this.syncLock)
            {
                this.pendingNext = next;

                if (this.timerRunning)
                    return;

                this.timerRunning = true;
                this.timerCts = new CancellationTokenSource();
                var token = this.timerCts.Token;

                _ = Task
                    .Delay(TimeSpan.FromMilliseconds(this.MillisecondsDelay), token)
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
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        public void Dispose()
        {
            lock (this.syncLock)
            {
                this.timerCts?.Cancel();
                this.timerCts?.Dispose();
                this.timerCts = null;
                this.pendingNext = null;
                this.timerRunning = false;
            }
        }
    }

    record ThrottleState(long MillisecondsDelay)
    {
        readonly Lock syncLock = new();
        bool inCooldown;

        public bool TryExecute(ILogger logger)
        {
            lock (this.syncLock)
            {
                if (this.inCooldown)
                    return false;

                this.inCooldown = true;
                _ = Task
                    .Delay(TimeSpan.FromMilliseconds(this.MillisecondsDelay))
                    .ContinueWith(_ =>
                    {
                        lock (this.syncLock)
                        {
                            this.inCooldown = false;
                        }
                    }, TaskScheduler.Default);

                return true;
            }
        }
    }

    readonly ConcurrentDictionary<string, SampleState> sampleStates = new();
    readonly ConcurrentDictionary<string, ThrottleState> throttleStates = new();

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue<long> is safe for trimming")]
    public Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var sampleAttribute = context.GetHandlerAttribute<SampleAttribute>();
        if (sampleAttribute is { Milliseconds: > 0 })
        {
            var handlerType = context.MessageHandler?.GetType().FullName ?? "unknown";
            var eventType = context.Message.GetType().FullName ?? "unknown";
            var key = $"{eventType}::{handlerType}";

            logger.LogDebug("Sampling event {EventType} for handler {HandlerType} with {Milliseconds}ms window", eventType, handlerType, sampleAttribute.Milliseconds);

            var state = this.sampleStates.GetOrAdd(key, _ => new SampleState(sampleAttribute.Milliseconds));
            state.Enqueue(next, logger);

            return Task.CompletedTask;
        }


        var throttleAttribute = context.GetHandlerAttribute<ThrottleAttribute>();
        if (throttleAttribute is { Milliseconds: > 0 })
        {
            var handlerType = context.MessageHandler?.GetType().FullName ?? "unknown";
            var eventType = context.Message.GetType().FullName ?? "unknown";
            var key = $"{eventType}::{handlerType}";

            var state = this.throttleStates.GetOrAdd(key, _ => new ThrottleState(throttleAttribute.Milliseconds));

            if (state.TryExecute(logger))
            {
                logger.LogDebug("Throttle executing event {EventType} for handler {HandlerType}", eventType, handlerType);
                return next();
            }

            logger.LogDebug("Throttle discarding event {EventType} for handler {HandlerType} - in cooldown", eventType, handlerType);
            return Task.CompletedTask;
        }

        return next();
    }
}
