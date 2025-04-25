namespace Shiny.Mediator.Infrastructure;


public class SentryRequestMiddleware<TRequest, TResult>(IHub? hub = null) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    // fingerprint vs span set
    public async Task<TResult> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
        if (hub == null)
            return await next().ConfigureAwait(false);
        
        var transaction = hub.StartTransaction("mediator", "request");
        var span = transaction.StartChild(context.MessageHandler.GetType().FullName!);
        
        var requestKey = ContractUtils.GetRequestKey(context.Message!);
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