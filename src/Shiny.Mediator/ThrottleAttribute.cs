namespace Shiny.Mediator;

/// <summary>
/// Applies a debounce/throttle to an event handler method. When events are published rapidly,
/// only the last event within the specified time window will be processed. Previous pending
/// events are discarded each time a new event arrives before the delay expires.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
/// <param name="milliseconds">The debounce delay in milliseconds. Only the last event after this period of inactivity is executed.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ThrottleAttribute(long milliseconds) : MediatorMiddlewareAttribute
{
    /// <summary>
    /// Gets the debounce delay in milliseconds.
    /// </summary>
    public long Milliseconds => milliseconds;
}
