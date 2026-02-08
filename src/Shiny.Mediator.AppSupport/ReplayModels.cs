namespace Shiny.Mediator;

/// <summary>
/// Enables replay caching for a stream request handler method. When applied, the middleware stores
/// the last emitted value from the stream and immediately replays it to new subscribers before
/// continuing with live data.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ReplayStreamAttribute : MediatorMiddlewareAttribute;

