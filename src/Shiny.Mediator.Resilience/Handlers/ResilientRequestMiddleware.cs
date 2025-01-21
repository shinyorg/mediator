using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Resilience.Handlers;


public class ResilientRequestHandlerMiddleware<TRequest, TResult>(
    ILogger<ResilientRequestHandlerMiddleware<TRequest, TResult>> logger,
    IConfiguration configuration,
    ResiliencePipelineProvider<string> pipelineProvider
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        RequestContext<TRequest> context,
        RequestHandlerDelegate<TResult> next 
    )
    {
        ResiliencePipeline? pipeline = null;
        var section = configuration.GetHandlerSection("Resilience", context.Request!, context.RequestHandler);
        
        if (section != null)
        {
            pipeline = pipelineProvider.GetPipeline(section.Key.ToLower());
        }
        else
        {
            var attribute = context.RequestHandler.GetHandlerHandleMethodAttribute<TRequest, ResilientAttribute>();
            if (attribute != null)
                pipeline = pipelineProvider.GetPipeline(attribute.ConfigurationKey.ToLower());
        }
        if (pipeline == null)
            return await next().ConfigureAwait(false);

        // it can't cancel properly here... may need to make next take a CancellationToken
        logger.LogDebug("Resilience Enabled - {Request}", context.Request);
        var result = await pipeline
            .ExecuteAsync(async _ => await next(), context.CancellationToken)
            .ConfigureAwait(false);
        
        return result;
    }
}