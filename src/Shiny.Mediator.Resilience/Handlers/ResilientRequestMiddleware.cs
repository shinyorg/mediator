using System.Reflection;
using Microsoft.Extensions.Configuration;
using Polly.Registry;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Resilience.Handlers;

/*
services.AddResiliencePipeline(key, static builder =>
   {
       // See: https://www.pollydocs.org/strategies/retry.html
       builder.AddRetry(new RetryStrategyOptions
       {
           ShouldHandle = new PredicateBuilder().Handle<TimeoutRejectedException>()
       });
   
       // See: https://www.pollydocs.org/strategies/timeout.html
       builder.AddTimeout(TimeSpan.FromSeconds(1.5));
   });
 */
//https://learn.microsoft.com/en-us/dotnet/core/resilience/?tabs=dotnet-cli
//https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/
public class ResilientRequestHandlerMiddleware<TRequest, TResult>(
    IConfiguration config,
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
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, ResilientAttribute>();
        attribute ??= request.GetType().GetCustomAttribute<ResilientAttribute>();
        if (attribute == null)
            return await next().ConfigureAwait(false);

        var pipeline = pipelineProvider.GetPipeline(attribute.ConfigurationKey.ToLower());
        
        // it can't cancel properly here... may need to make next take a CancellationToken
        var result = await pipeline
            .ExecuteAsync(async _ => await next(), cancellationToken)
            .ConfigureAwait(false);
        
        return result;
    }
}