using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Sentry;

/// <summary>
/// Request middleware that opens a Sentry transaction and child span for every dispatched request,
/// records the contract key, and attaches the final context headers to the span.
/// </summary>
public class SentryRequestMiddleware<TRequest, TResult>(IContractKeyProvider contractKeyProvider) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    // fingerprint vs span set
    /// <inheritdoc/>
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

/// <summary>
/// Command middleware that opens a Sentry transaction and child span for every dispatched command
/// and attaches the final context headers to the span.
/// </summary>
public class SentryCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
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

/// <summary>
/// Event middleware that opens a Sentry transaction and child span around each event handler invocation
/// and attaches the final context headers to the span.
/// </summary>
public class SentryEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    // would be nice to see a transaction across the event spray
    /// <inheritdoc/>
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

/// <summary>
/// Stream request middleware that opens a Sentry transaction for the stream and a child span
/// for each <c>MoveNextAsync</c> iteration, so per-yield latency is visible in Sentry.
/// </summary>
public class SentryStreamRequestMiddleware<TRequest, TResult>(IContractKeyProvider contractKeyProvider) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    /// <inheritdoc/>
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
        try
        {
            while (await nxt.MoveNextAsync() && !cancellationToken.IsCancellationRequested)
            {
                yield return nxt.Current;
                moveSpan.Finish();
                moveSpan = span.StartChild("movenext");
            }
        }
        finally
        {
            await nxt.DisposeAsync().ConfigureAwait(false);
        }
        span.Finish();
        transaction.Finish();
    }
}


/// <summary>
/// Exception handler that forwards any unhandled exception from mediator dispatch to
/// <see cref="SentrySdk.CaptureException(Exception)"/>. Returns <c>false</c> so the exception
/// continues to propagate through the pipeline.
/// </summary>
public class SentryExceptionHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        SentrySdk.CaptureException(exception);
        return Task.FromResult(false);
    }
}
