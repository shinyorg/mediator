namespace Shiny.Mediator.Infrastructure;


public class SentryCommandMiddleware<TCommand>(IHub hub) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(IMediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        var transaction = hub.StartTransaction("mediator", "event");
        var span = transaction.StartChild(context.MessageHandler.GetType().FullName!);
        await next().ConfigureAwait(false);
        foreach (var header in context.Headers)
            span.SetData(header.Key, header.Value);
        
        span.Finish();
        transaction.Finish();
    }
}