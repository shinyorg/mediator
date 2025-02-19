namespace Shiny.Mediator.Infrastructure;


public class SentryRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
{
    // fingerprint vs span set
    public async Task<TResult> Process(
        RequestContext<TRequest> context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
        var transaction = SentrySdk.StartTransaction("mediator", "request");
        var span = transaction.StartChild(context.Handler.GetType().FullName!);
        
        var requestKey = Utils.GetRequestKey(context.Request!);
        span.SetData("RequestKey", requestKey);

        var result = await next().ConfigureAwait(false);

        // tap headers on to span AFTER request - do we care if this changed before vs after? can I denote this in sentry?
        foreach (var header in context.Values)
            span.SetData(header.Key, header.Value);

        span.Finish();
        transaction.Finish();
        
        return result;
    }
}