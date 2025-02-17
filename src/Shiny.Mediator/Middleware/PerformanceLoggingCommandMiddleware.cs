using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class PerformanceLoggingCommandMiddleware<TCommand>(
    IConfiguration configuration,
    ILogger<TCommand> logger
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(
        CommandContext<TCommand> context, 
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        var section = configuration.GetHandlerSection("PerformanceLogging", context.Command!, context.Handler);
        if (section == null)
        {
            await next().ConfigureAwait(false);
            return;
        }

        var millis = section.GetValue("ErrorThresholdMilliseconds", 5000);
        var ts = TimeSpan.FromMilliseconds(millis);

        var startTime = Stopwatch.GetTimestamp();
        await next();
        var delta = Stopwatch.GetElapsedTime(startTime);
        
        if (delta > ts)
        {
            context.SetPerformanceLoggingThresholdBreached(delta);
            logger.LogError(
                "{CommandType} took longer than {Threshold} to execute - {Elapsed}", 
                typeof(TCommand), 
                ts,
                delta
            );
        }
        else if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "{CommandType} took {Elapsed} to execute",
                typeof(TCommand),
                delta
            );
        }
    }
}

