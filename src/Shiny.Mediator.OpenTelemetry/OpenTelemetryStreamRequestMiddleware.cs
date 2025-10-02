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
        using var activity = ActivitySource.StartActivity("mediator.stream", ActivityKind.Internal);
        activity?.SetTag("handler.type", context.MessageHandler!.GetType().FullName!);
        var nxt = next().GetAsyncEnumerator(cancellationToken);
        
        var requestKey = contractKeyProvider.GetContractKey(context.Message!);
        activity?.SetTag("RequestKey", requestKey);
        
        var moveActivity = activity != null ? ActivitySource.StartActivity("initial_movenext", ActivityKind.Internal, activity.Context) : null;
        while (await nxt.MoveNextAsync() && !cancellationToken.IsCancellationRequested)
        {
            yield return nxt.Current;
            moveActivity?.Dispose();
            moveActivity = activity != null ? ActivitySource.StartActivity("movenext", ActivityKind.Internal, activity.Context) : null;
        }
    }
}