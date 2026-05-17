using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Stream middleware that repeats the underlying handler on a fixed interval. The interval (in seconds) is
/// resolved from the context header first, then <c>Mediator:TimerRefresh</c> configuration, then a
/// <see cref="TimerRefreshAttribute"/> on the handler method. When the interval is zero or negative, the
/// middleware passes through without iterating.
/// </summary>
public class TimerRefreshStreamRequestMiddleware<TRequest, TResult>(
    ILogger<TimerRefreshStreamRequestMiddleware<TRequest, TResult>> logger,
    IConfiguration configuration
) : IStreamRequestMiddleware<TRequest, TResult>
    where TRequest : IStreamRequest<TResult>
{
    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue<int> is safe for trimming")]
    public IAsyncEnumerable<TResult> Process(
        IMediatorContext context, 
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var interval = 0;

        var header = context.TryGetTimerRefresh();
        if (header != null)
        {
            logger.LogDebug("Timer setting in MediatorContext");
            interval = header.Value;
        }
        else
        {
            var section = configuration.GetHandlerSection("TimerRefresh", context.Message, context.MessageHandler);
            if (section != null)
            {
                interval = section.GetValue("IntervalSeconds", 0);
                logger.LogDebug("Timer setting found in configuration");
            }
            else
            {
                var attribute = context.GetHandlerAttribute<TimerRefreshAttribute>();
                if (attribute != null)
                {
                    interval = attribute.IntervalSeconds;
                    logger.LogDebug("Timer setting found on attribute");
                }
            }
        }

        logger.LogDebug("Timer Setting Interval: {value}", interval);
        if (interval <= 0)
        {
            logger.LogDebug("Timer Refresh will not be used - returning");
            return next();
        }

        logger.LogDebug("Timer Refresh Set to run");
        return this.Iterate(interval, next, cancellationToken);
    }


    async IAsyncEnumerable<TResult> Iterate(
        int refreshSeconds, 
        StreamRequestHandlerDelegate<TResult> next, 
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var ts = TimeSpan.FromSeconds(refreshSeconds);
        
        while (!ct.IsCancellationRequested)
        {
            // fire initial before waiting
            logger.LogDebug("Firing Timer Request");
            var nxt = next().GetAsyncEnumerator(ct);
            try
            {
                while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
                {
                    yield return nxt.Current;
                    logger.LogDebug("Firing Timer Response");
                }
            }
            finally
            {
                await nxt.DisposeAsync().ConfigureAwait(false);
            }

            // TODO: number of iterations configuration?
            logger.LogDebug("Waiting for next iteration");
            await Task.Delay(ts, ct).ConfigureAwait(false);
        }
    }
}