using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class TimedLoggingMiddlewareConfig
{
    public bool LogAll { get; set; }
    public TimeSpan? ErrorThreshold { get; set; }
}

public class TimedLoggingRequestMiddleware<TRequest, TResult>(ILogger<TRequest> logger, TimedLoggingMiddlewareConfig config) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(TRequest request, Func<Task<TResult>> next, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();
        var result = await next();
        sw.Stop();

        var errored = config.ErrorThreshold != null && config.ErrorThreshold < sw.Elapsed;
        var msg = $"{typeof(TRequest)} pipeline execution took ${sw.Elapsed}";
        
        if (!errored && config.LogAll)
        {
            logger.LogDebug(msg);
        }
        if (errored)
        {
            logger.LogError(msg);
        }
        return result;
    }
}

