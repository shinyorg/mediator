using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class TimedLoggingRequestMiddleware<TRequest, TResult>(ILogger<TRequest> logger) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(TRequest request, RequestHandlerDelegate<TResult> next, IRequestHandler requestHandler, CancellationToken cancellationToken)
    {
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, TimedLoggingAttribute>();
        if (attribute == null)
            return await next().ConfigureAwait(false);

        var sw = Stopwatch.StartNew();
        var result = await next();
        sw.Stop();

        var ts = TimeSpan.FromMilliseconds(attribute.ErrorThresholdMillis);
        if (attribute.ErrorThresholdMillis > 0 && sw.Elapsed > ts)
            logger.LogError("{RequestType} took longer than {Threshold} to execute - {Elapsed}", typeof(TRequest), ts, sw.Elapsed);

        else if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("{RequestType} took {Elapsed} to execute", typeof(TRequest), sw.Elapsed);
        
        return result;
    }
}

