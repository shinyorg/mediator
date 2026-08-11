using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace Shiny.Mediator.Resilience.Handlers;


/// <summary>
/// Request middleware that wraps the downstream handler in a named Polly
/// <see cref="ResiliencePipeline"/> resolved from either the handler's <c>Resilience</c>
/// configuration section or a <see cref="ResilientAttribute"/>. When no pipeline is configured,
/// the request flows through unchanged.
/// </summary>
public class ResilientRequestMiddleware<TRequest, TResult>(
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<ResilientRequestMiddleware<TRequest, TResult>>? logger = null,
    IConfiguration? configuration = null
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    /// <inheritdoc/>
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        ResiliencePipeline? pipeline = null;
        var section = context.GetHandlerSection(configuration, "Resilience");

        if (section != null)
        {
            pipeline = pipelineProvider.GetPipeline(section.Key.ToLower());
        }
        else
        {
            var attribute = context.GetHandlerAttribute<ResilientAttribute>();
            if (attribute != null)
                pipeline = pipelineProvider.GetPipeline(attribute.ConfigurationKey.ToLower());
        }
        if (pipeline == null)
            return await next().ConfigureAwait(false);

        // it can't cancel properly here... may need to make next take a CancellationToken
        logger?.LogDebug("Resilience Enabled - {Request}", context.Message);
        var result = await pipeline
            .ExecuteAsync(async _ => await next(), cancellationToken)
            .ConfigureAwait(false);

        return result;
    }
}
