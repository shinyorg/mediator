using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Shiny.Mediator.Resilience.Handlers;

namespace Shiny.Mediator;


/// <summary>
/// Registration extensions for the Polly-based resilience middleware.
/// </summary>
public static class ResilientExtensions
{
    /// <summary>
    /// Registers one or more named Polly resilience pipelines and wires the request and command
    /// resilience middleware into the mediator pipeline. Pipelines are looked up by the lower-cased
    /// configuration key referenced from <see cref="ResilientAttribute"/> or a handler's
    /// <c>Resilience</c> configuration section.
    /// </summary>
    /// <param name="mediatorBuilder">The mediator builder to wire into.</param>
    /// <param name="rbuilders">Named pipeline builders to register.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ShinyMediatorBuilder AddResiliencyMiddleware(this ShinyMediatorBuilder mediatorBuilder, params (string Key, Action<ResiliencePipelineBuilder> Builder)[] rbuilders)
    {
        foreach (var rbs in rbuilders)
            mediatorBuilder.Services.AddResiliencePipeline(rbs.Key.ToLower(), builder => rbs.Builder.Invoke(builder));

        mediatorBuilder.AddOpenRequestMiddleware(typeof(ResilientRequestMiddleware<,>));
        mediatorBuilder.AddOpenCommandMiddleware(typeof(ResilientCommandMiddleware<>));
        return mediatorBuilder;
    }


    /// <summary>
    /// Registers Polly resilience pipelines described by the <c>Resilience</c> section of the supplied
    /// <see cref="IConfiguration"/> and wires the request and command resilience middleware. Each
    /// child key produces a named pipeline configured from <see cref="ResilienceConfig"/>.
    /// </summary>
    /// <param name="mediatorBuilder">The mediator builder to wire into.</param>
    /// <param name="configuration">Configuration root containing the <c>Resilience</c> section.</param>
    /// <returns>The same builder for chaining.</returns>
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


/// <summary>
/// Configuration-bound description of a single Polly resilience pipeline composed of optional
/// timeout and retry strategies.
/// </summary>
public class ResilienceConfig
{
    /// <summary>
    /// Optional timeout in milliseconds applied to each execution.
    /// </summary>
    public double? TimeoutMilliseconds { get; init; }

    /// <summary>
    /// Optional retry strategy applied to <see cref="TimeoutRejectedException"/> failures.
    /// </summary>
    public RetryStrategyConfig? Retry { get; init; }
    // public CircuitBreakerConfig? CircuitBreaker { get; init; }
}

/// <summary>
/// Configuration-bound description of a Polly retry strategy: attempt count, delay, backoff type,
/// and jitter.
/// </summary>
public class RetryStrategyConfig
{
    /// <summary>
    /// Maximum number of retry attempts. Defaults to <c>3</c>.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Backoff type applied between attempts. Defaults to <see cref="DelayBackoffType.Constant"/>.
    /// </summary>
    public DelayBackoffType BackoffType { get; init; } = DelayBackoffType.Constant;

    /// <summary>
    /// When <c>true</c>, randomized jitter is added to each retry delay.
    /// </summary>
    public bool UseJitter { get; init; }

    /// <summary>
    /// Base delay between attempts in milliseconds. Defaults to <c>3000</c>.
    /// </summary>
    public int DelayMilliseconds { get; init; } = 3000;
}

// public class CircuitBreakerConfig
// {
//
// }
