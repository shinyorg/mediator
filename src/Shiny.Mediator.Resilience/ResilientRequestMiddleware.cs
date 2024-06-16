using Polly.Registry;

namespace Shiny.Mediator.Resilience;


[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ResilientAttribute(string configurationKey) : Attribute
{
    public string ConfigurationKey => configurationKey;
}

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
public class ResilientRequestHandlerMiddleware<TRequest, TResult>(ResiliencePipelineProvider<string> pipelineProvider) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, TResult, ResilientAttribute>();
        if (attribute == null)
            return await next().ConfigureAwait(false);

        var pipeline = pipelineProvider.GetPipeline<TResult>(attribute.ConfigurationKey);
        var result = await pipeline
            .ExecuteAsync(async _ => await next(), cancellationToken)
            .ConfigureAwait(false);
        
        return result;
    }
}