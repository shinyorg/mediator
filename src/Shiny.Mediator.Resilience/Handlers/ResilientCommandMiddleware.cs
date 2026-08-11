using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace Shiny.Mediator.Resilience.Handlers;


/// <summary>
/// Command middleware that wraps the downstream handler in a named Polly
/// <see cref="ResiliencePipeline"/> resolved from either the handler's <c>Resilience</c>
/// configuration section or a <see cref="ResilientAttribute"/>. When no pipeline is configured,
/// the command flows through unchanged.
/// </summary>
public class ResilientCommandMiddleware<TCommand>(
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<ResilientCommandMiddleware<TCommand>>? logger = null,
    IConfiguration? configuration = null
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    /// <inheritdoc/>
    public async Task Process(
        IMediatorContext context,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken
    )
    {
        ResiliencePipeline? pipeline = null;
        var section = configuration.GetHandlerSection("Resilience", context.Message, context.MessageHandler);

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
        {
            await next().ConfigureAwait(false);
            return;
        }

        // it can't cancel properly here... may need to make next take a CancellationToken
        logger?.LogDebug("Resilience Enabled - {Request}", context.Message);
        await pipeline
            .ExecuteAsync(async _ => await next(), cancellationToken)
            .ConfigureAwait(false);
    }
}
