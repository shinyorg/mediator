using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class TimedLoggingRequestMiddleware<TRequest, TResult>(ILogger<TRequest> logger) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(TRequest request, RequestHandlerDelegate<TResult> next, IRequestHandler<TRequest, TResult> requestHandler, CancellationToken cancellationToken)
    {
        var attribute = requestHandler.GetType().GetCustomAttribute<TimedLoggingAttribute>();
        if (attribute == null)
            return await next().ConfigureAwait(false);

        var sw = Stopwatch.StartNew();
        var result = await next();
        sw.Stop();

        var msg = $"{typeof(TRequest)} pipeline execution took ${sw.Elapsed}";
        var ts = TimeSpan.FromMilliseconds(attribute.ErrorThresholdMillis);
        if (attribute.ErrorThresholdMillis > 0 && sw.Elapsed > ts)
            logger.LogDebug(msg);

        else if (logger.IsEnabled(LogLevel.Debug))
            logger.LogError(msg);
        
        return result;
    }
}

