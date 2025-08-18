using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Sentry;

public class SentryRequestMiddleware<TRequest, TResult>(IContractKeyProvider contractKeyProvider) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    // fingerprint vs span set
    public async Task<TResult> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
        var transaction = SentrySdk.StartTransaction("mediator", "request");
        var span = transaction.StartChild(context.MessageHandler!.GetType().FullName!);
        
        var requestKey = contractKeyProvider.GetContractKey(context.Message!);
        span.SetData("RequestKey", requestKey);

        var result = await next().ConfigureAwait(false);

        // tap headers on to span AFTER request - do we care if this changed before vs after? can I denote this in sentry?
        foreach (var header in context.Headers)
            span.SetData(header.Key, header.Value);

        span.Finish();
        transaction.Finish();
        
        return result;
    }
}

public class SentryCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(IMediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        var transaction = SentrySdk.StartTransaction("mediator", "event");
        var span = transaction.StartChild(context.MessageHandler!.GetType().FullName!);
        await next().ConfigureAwait(false);
        foreach (var header in context.Headers)
            span.SetData(header.Key, header.Value);
        
        span.Finish();
        transaction.Finish();
    }
}

public class SentryEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    // would be nice to see a transaction across the event spray
    public async Task Process(
        IMediatorContext context, 
        EventHandlerDelegate next, 
        CancellationToken cancellationToken
    )
    {
        var transaction = SentrySdk.StartTransaction("mediator", "event");
        var span = transaction.StartChild(context.MessageHandler!.GetType().FullName!);
        
        await next().ConfigureAwait(false);
        foreach (var header in context.Headers)
            span.SetData(header.Key, header.Value);
        
        span.Finish();
        transaction.Finish();
    }
}

public class SentryStreamRequestMiddleware<TRequest, TResult>(IContractKeyProvider contractKeyProvider) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public async IAsyncEnumerable<TResult> Process(
        IMediatorContext context, 
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var transaction = SentrySdk.StartTransaction("mediator", "stream");
        var span = transaction.StartChild(context.MessageHandler!.GetType().FullName!);
        var nxt = next().GetAsyncEnumerator(cancellationToken);
        
        var requestKey = contractKeyProvider.GetContractKey(context.Message!);
        span.SetData("RequestKey", requestKey);
        
        var moveSpan = span.StartChild("initial_movenext");
        while (await nxt.MoveNextAsync() && !cancellationToken.IsCancellationRequested)
        {
            yield return nxt.Current;
            moveSpan.Finish();
            moveSpan = span.StartChild("movenext");
        }
        span.Finish();
        transaction.Finish();
    }
}


public class SentryExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        SentrySdk.CaptureException(exception);
        return Task.FromResult(false);
    }
}