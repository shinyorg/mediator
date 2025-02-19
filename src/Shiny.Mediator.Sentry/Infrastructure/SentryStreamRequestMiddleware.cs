namespace Shiny.Mediator.Infrastructure;


public class SentryStreamRequestMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public async IAsyncEnumerable<TResult> Process(
        RequestContext<TRequest> context, 
        StreamRequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        var transaction = SentrySdk.StartTransaction("mediator", "stream");
        var span = transaction.StartChild(context.Handler.GetType().FullName!);
        var nxt = next().GetAsyncEnumerator(cancellationToken);
        
        var requestKey = Utils.GetRequestKey(context.Request!);
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