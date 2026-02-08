namespace Shiny.Mediator;


/// <summary>
/// Applies a Polly resilience policy (retry, timeout, circuit breaker) to a handler method.
/// The policy is resolved by the specified configuration key from the resilience pipeline configuration.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
/// <param name="configurationKey">The configuration key used to resolve the resilience pipeline from settings.</param>
[AttributeUsage(AttributeTargets.Method)]
public class ResilientAttribute(string configurationKey) : MediatorMiddlewareAttribute
{
    /// <summary>
    /// Gets the configuration key used to resolve the resilience pipeline.
    /// </summary>
    public string ConfigurationKey => configurationKey;
}