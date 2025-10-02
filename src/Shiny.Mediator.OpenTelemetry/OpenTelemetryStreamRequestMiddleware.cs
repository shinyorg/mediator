using System.Diagnostics;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryStreamRequestMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult> 
    where TRequest : IStreamRequest<TResult>
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator", "1.0.0");
    private readonly IContractKeyProvider _contractKeyProvider;

    public OpenTelemetryStreamRequestMiddleware(IContractKeyProvider contractKeyProvider)
    {
        _contractKeyProvider = contractKeyProvider;
    }

    public async IAsyncEnumerable<TResult> Process(
        IMediatorContext context, 
        StreamRequestHandlerDelegate<TResult> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        using var activity = ActivitySource.StartActivity("mediator.stream", ActivityKind.Internal);
        
        if (activity != null)
        {
            activity.SetTag("handler.type", context.MessageHandler?.GetType().FullName);
            
            var requestKey = _contractKeyProvider.GetContractKey(context.Message!);
            activity.SetTag("request.key", requestKey);
            
            foreach (var header in context.Headers)
            {
                activity.SetTag($"context.header.{header.Key}", header.Value);
            }
        }

        var itemCount = 0;
        Activity? itemActivity = null;

        try
        {
            var nxt = next();
            await foreach (var item in nxt.WithCancellation(cancellationToken))
            {
                itemActivity?.Dispose();
                itemActivity = ActivitySource.StartActivity("mediator.stream.item", ActivityKind.Internal, activity?.Context ?? default);
                itemActivity?.SetTag("item.index", itemCount++);
                
                yield return item;
            }
            
            activity?.SetTag("stream.item_count", itemCount);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            itemActivity?.Dispose();
        }
    }
}