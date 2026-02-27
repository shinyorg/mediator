namespace Shiny.Mediator;

/// <summary>
/// Applies true throttle behavior to an event handler method. The first event executes immediately,
/// then all subsequent events within the cooldown window are discarded. Once the cooldown expires,
/// the next event will execute immediately again.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
/// <param name="milliseconds">The throttle cooldown duration in milliseconds.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ThrottleAttribute(long milliseconds) : MediatorMiddlewareAttribute
{
    /// <summary>
    /// Gets the throttle cooldown duration in milliseconds.
    /// </summary>
    public long Milliseconds => milliseconds;
}
