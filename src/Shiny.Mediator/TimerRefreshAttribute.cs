namespace Shiny.Mediator;

/// <summary>
/// Enables automatic periodic re-execution of a stream request handler at the specified interval.
/// When applied to a stream handler method, the middleware will repeatedly invoke the handler
/// and yield new results on the configured timer.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
/// <param name="intervalSeconds">The interval in seconds between each automatic refresh of the stream.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class TimerRefreshAttribute(int intervalSeconds) : MediatorMiddlewareAttribute
{
    /// <summary>
    /// Gets the interval in seconds between each automatic refresh.
    /// </summary>
    public int IntervalSeconds => intervalSeconds;
}