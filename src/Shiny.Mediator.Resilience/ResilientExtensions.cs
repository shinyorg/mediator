using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Shiny.Mediator.Resilience.Handlers;

namespace Shiny.Mediator;


public static class ResilientExtensions
{
    public static ShinyMediatorBuilder AddResiliencyMiddleware(this ShinyMediatorBuilder mediatorBuilder, params (string Key, Action<ResiliencePipelineBuilder> Builder)[] rbuilders)
    {
        foreach (var rbs in rbuilders)
            mediatorBuilder.Services.AddResiliencePipeline(rbs.Key.ToLower(), builder => rbs.Builder.Invoke(builder));

        mediatorBuilder.AddOpenRequestMiddleware(typeof(ResilientRequestMiddleware<,>));
        mediatorBuilder.AddOpenCommandMiddleware(typeof(ResilientCommandMiddleware<>));
        return mediatorBuilder;
    }


    public static ShinyMediatorBuilder AddResiliencyMiddleware(this ShinyMediatorBuilder mediatorBuilder, IConfiguration configuration)
    {
        var items = configuration.GetSection("Resilience").Get<Dictionary<string, ResilienceConfig>>();
        if (items != null)
        {
            foreach (var item in items)
            {
                mediatorBuilder.Services.AddResiliencePipeline(item.Key.ToLower(), builder =>
                {
                    if (item.Value.TimeoutMilliseconds != null)
                        builder.AddTimeout(TimeSpan.FromMilliseconds(item.Value.TimeoutMilliseconds.Value));

                    if (item.Value.Retry != null)
                    {
                        var r = item.Value.Retry;
                        var strategy = new RetryStrategyOptions
                        {
                            MaxRetryAttempts = r.MaxAttempts,
                            UseJitter = r.UseJitter,
                            Delay = TimeSpan.FromMilliseconds(r.DelayMilliseconds),
                            BackoffType = r.BackoffType,
                            ShouldHandle = new PredicateBuilder().Handle<TimeoutRejectedException>()
                        };
                        builder.AddRetry(strategy);
                    }
                    // if (item.Value.CircuitBreaker != null)
                    // {
                    //     var strategy = new CircuitBreakerStrategyOptions
                    //     {
                    //     };
                    //     builder.AddCircuitBreaker(strategy);
                    // }
                });
            }
        }
        return mediatorBuilder;
    }
}


public class ResilienceConfig
{
    public double? TimeoutMilliseconds { get; init; }
    public RetryStrategyConfig? Retry { get; init; }
    // public CircuitBreakerConfig? CircuitBreaker { get; init; }
}

public class RetryStrategyConfig
{
    public int MaxAttempts { get; init; } = 3;
    public DelayBackoffType BackoffType { get; init; } = DelayBackoffType.Constant;
    public bool UseJitter { get; init; }
    public int DelayMilliseconds { get; init; } = 3000;
}

// public class CircuitBreakerConfig
// {
//     
// }