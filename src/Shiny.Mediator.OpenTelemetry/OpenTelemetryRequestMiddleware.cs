using System.Diagnostics;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryRequestMiddleware<TRequest, TResult>(IContractKeyProvider contractKeyProvider) : IRequestMiddleware<TRequest, TResult> 
    where TRequest : IRequest<TResult>
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator");

    public async Task<TResult> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
        var transaction = ActivitySource.StartActivity("mediator", ActivityKind.Internal);
        var span = transaction != null ? ActivitySource.StartActivity(context.MessageHandler!.GetType().FullName!, ActivityKind.Internal, transaction.Context) : null;
        
        var requestKey = contractKeyProvider.GetContractKey(context.Message!);
        span?.SetTag("RequestKey", requestKey);

        var result = await next().ConfigureAwait(false);

        // tap headers on to span AFTER request - do we care if this changed before vs after? can I denote this in opentelemetry?
        foreach (var header in context.Headers)
            span?.SetTag(header.Key, header.Value);

        span?.Dispose();
        transaction?.Dispose();
        
        return result;
    }
}