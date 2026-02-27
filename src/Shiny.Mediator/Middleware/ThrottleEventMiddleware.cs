using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


[MiddlewareOrder(50)]
public class ThrottleEventMiddleware<TEvent>(
    ILogger<ThrottleEventMiddleware<TEvent>> logger,
    IConfiguration configuration
) : IEventMiddleware<TEvent> where TEvent : IEvent
{
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

        var state = this.states.GetOrAdd(key, _ => new ThrottleState(milliseconds));

        if (state.TryExecute(logger))
        {
            logger.LogDebug("Throttle executing event {EventType} for handler {HandlerType}", eventType, handlerType);
            return next();
        }

        logger.LogDebug("Throttle discarding event {EventType} for handler {HandlerType} - in cooldown", eventType, handlerType);
        return Task.CompletedTask;
    }
}
