using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Shiny.Mediator.Resilience.Handlers;

namespace Shiny.Mediator;


public static class ResilientExtensions
{
    public static ShinyConfigurator AddResiliencyMiddleware(this ShinyConfigurator configurator, params (string Key, Action<ResiliencePipelineBuilder> Builder)[] rbuilders)
    {
        foreach (var rbs in rbuilders)
            configurator.Services.AddResiliencePipeline(rbs.Key.ToLower(), builder => rbs.Builder.Invoke(builder));

        configurator.AddOpenRequestMiddleware(typeof(ResilientRequestHandlerMiddleware<,>));
        return configurator;
    }


    public static ShinyConfigurator AddResiliencyMiddleware(this ShinyConfigurator configurator, IConfiguration configuration)
    {
        
/*
"*": {
"RetryCount": 3,
"RetryDelay": 2000,
"CircuitBreakerCount": 5,
"CircuitBreakerDelay": 5000
}        
*/
        var items = configuration.GetSection("Resilience").Get<Dictionary<string, ResilienceConfig>>();
        if (items != null)
        {
            foreach (var item in items)
            {
                configurator.Services.AddResiliencePipeline(item.Key.ToLower(), builder =>
                {
                    if (item.Value.MaxRetries != null)
                    {
                        var strategy = new RetryStrategyOptions
                        {
                            MaxRetryAttempts = item.Value.MaxRetries.Value,
                            UseJitter = item.Value.UseJitter,
                            ShouldHandle = new PredicateBuilder().Handle<TimeoutRejectedException>()
                        };
                        if (item.Value.RetryDelay != null)
                            strategy.Delay = TimeSpan.FromSeconds(item.Value.RetryDelay.Value);

                        if (item.Value.BackoffType != null)
                            strategy.BackoffType = item.Value.BackoffType.Value;
                        
                        builder.AddRetry(strategy);
                    }

                    if (item.Value.TimeoutMilliseconds != null)
                        builder.AddTimeout(TimeSpan.FromMilliseconds(item.Value.TimeoutMilliseconds.Value));
                    
                    // builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions())
                });
            }
        }
        return configurator;
    }
}


public class ResilienceConfig
{
    public double? TimeoutMilliseconds { get; init; }
    public int? MaxRetries { get; init; }
    public int? RetryDelay { get; init; }
    public DelayBackoffType? BackoffType { get; init; }
    public bool UseJitter { get; init; }
}