namespace Shiny.Mediator.Infrastructure;


public class SentryCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(MediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        var transaction = SentrySdk.StartTransaction("mediator", "event");
        var span = transaction.StartChild(context.MessageHandler.GetType().FullName!);
        await next().ConfigureAwait(false);
        foreach (var header in context.Values)
            span.SetData(header.Key, header.Value);
        
        span.Finish();
        transaction.Finish();
    }
}