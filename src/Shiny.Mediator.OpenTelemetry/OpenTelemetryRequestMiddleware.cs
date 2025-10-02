using System.Diagnostics;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.OpenTelemetry;

public class OpenTelemetryRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult> 
    where TRequest : IRequest<TResult>
{
    private static readonly ActivitySource ActivitySource = new("Shiny.Mediator", "1.0.0");
    private readonly IContractKeyProvider _contractKeyProvider;

    public OpenTelemetryRequestMiddleware(IContractKeyProvider contractKeyProvider)
    {
        _contractKeyProvider = contractKeyProvider;
    }

    public async Task<TResult> Process(
        IMediatorContext context, 
        RequestHandlerDelegate<TResult> next, 
        CancellationToken cancellationToken
    )
    {
        using var activity = ActivitySource.StartActivity("mediator.request", ActivityKind.Internal);
        
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

        try
        {
            var result = await next().ConfigureAwait(false);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}