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
        using var activity = ActivitySource.StartActivity("mediator.request", ActivityKind.Internal);
        
        var requestKey = contractKeyProvider.GetContractKey(context.Message!);
        activity?.SetTag("RequestKey", requestKey);

        var result = await next().ConfigureAwait(false);

        // tap headers on to activity AFTER request - do we care if this changed before vs after? can I denote this in opentelemetry?
        foreach (var header in context.Headers)
            activity?.SetTag(header.Key, header.Value);
        
        return result;
    }
}