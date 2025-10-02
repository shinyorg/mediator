using System.Diagnostics;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> 
    where TCommand : ICommand
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator");

    public async Task Process(
        IMediatorContext context, 
        CommandHandlerDelegate next, 
        CancellationToken cancellationToken
    )
    {
        using var activity = ActivitySource.StartActivity("mediator.command", ActivityKind.Internal);
        activity?.SetTag("handler.type", context.MessageHandler!.GetType().FullName!);
        await next().ConfigureAwait(false);
        foreach (var header in context.Headers)
            activity?.SetTag(header.Key, header.Value);
    }
}