using System.Diagnostics;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryStreamRequestMiddleware<TRequest, TResult>(IContractKeyProvider contractKeyProvider) : IStreamRequestMiddleware<TRequest, TResult> 
    where TRequest : IStreamRequest<TResult>
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator");

    public async IAsyncEnumerable<TResult> Process(
        IMediatorContext context, 
        StreamRequestHandlerDelegate<TResult> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var transaction = ActivitySource.StartActivity("mediator", ActivityKind.Internal);
        var span = transaction != null ? ActivitySource.StartActivity(context.MessageHandler!.GetType().FullName!, ActivityKind.Internal, transaction.Context) : null;
        var nxt = next().GetAsyncEnumerator(cancellationToken);
        
        var requestKey = contractKeyProvider.GetContractKey(context.Message!);
        span?.SetTag("RequestKey", requestKey);
        
        var moveSpan = span != null ? ActivitySource.StartActivity("initial_movenext", ActivityKind.Internal, span.Context) : null;
        while (await nxt.MoveNextAsync() && !cancellationToken.IsCancellationRequested)
        {
            yield return nxt.Current;
            moveSpan?.Dispose();
            moveSpan = span != null ? ActivitySource.StartActivity("movenext", ActivityKind.Internal, span.Context) : null;
        }
        span?.Dispose();
        transaction?.Dispose();
    }
}