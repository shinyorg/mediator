namespace Shiny.Mediator;


/// <summary>
/// Enables offline availability for a request handler method. When the request fails (e.g. no network),
/// the middleware returns the last successfully cached result from persistent storage. Successful results
/// are automatically stored for future offline use.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class OfflineAvailableAttribute : MediatorMiddlewareAttribute;

/// <summary>
/// Provides context information when a result was served from offline storage.
/// </summary>
/// <param name="RequestKey">The cache key used to store and retrieve the offline result.</param>
/// <param name="Timestamp">The timestamp when the offline result was originally stored.</param>
public record OfflineAvailableContext(
    string RequestKey,
    DateTimeOffset Timestamp
);