using System.Diagnostics;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> 
    where TCommand : ICommand
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator", "1.0.0");

    public async Task Process(
        IMediatorContext context, 
        CommandHandlerDelegate next, 
        CancellationToken cancellationToken
    )
    {
        using var activity = ActivitySource.StartActivity("mediator.command", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("handler.type", context.MessageHandler?.GetType().FullName);
            
            foreach (var header in context.Headers)
            {
                activity.SetTag($"context.header.{header.Key}", header.Value);
            }
        }

        try
        {
            await next().ConfigureAwait(false);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}