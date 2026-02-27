namespace Shiny.Mediator;

/// <summary>
/// Applies a sample (fixed-window) behavior to an event handler method. When the first event arrives,
/// a timer starts. Subsequent events within the window replace the pending delegate but do not reset the timer.
/// When the timer fires at the end of the window, the last event received is executed.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
/// <param name="milliseconds">The sample window duration in milliseconds.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SampleAttribute(long milliseconds) : MediatorMiddlewareAttribute
{
    /// <summary>
    /// Gets the sample window duration in milliseconds.
    /// </summary>
    public long Milliseconds => milliseconds;
}
