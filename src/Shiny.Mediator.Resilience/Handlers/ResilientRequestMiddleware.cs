using System.Reflection;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Registry;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Resilience.Handlers;


public class ResilientRequestHandlerMiddleware<TRequest, TResult>(
    IConfiguration configuration,
    ResiliencePipelineProvider<string> pipelineProvider
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        ResiliencePipeline? pipeline = null;
        var section = configuration.GetHandlerSection("Resilience", request!, requestHandler);
        
        if (section != null)
        {
            pipeline = pipelineProvider.GetPipeline(section.Key.ToLower());
        }
        else
        {
            var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, ResilientAttribute>();
            attribute ??= request.GetType().GetCustomAttribute<ResilientAttribute>();
            if (attribute != null)
                pipeline = pipelineProvider.GetPipeline(attribute.ConfigurationKey.ToLower());
        }
        if (pipeline == null)
            return await next().ConfigureAwait(false);

        // it can't cancel properly here... may need to make next take a CancellationToken
        var result = await pipeline
            .ExecuteAsync(async _ => await next(), cancellationToken)
            .ConfigureAwait(false);
        
        return result;
    }
}