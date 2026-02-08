namespace Shiny.Mediator;


/// <summary>
/// Enables caching for a request handler method. When applied, the middleware will cache the result
/// of the handler execution and return the cached value for subsequent identical requests until expiration.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class CacheAttribute : MediatorMiddlewareAttribute
{
    /// <summary>
    /// Gets or sets the absolute expiration time in seconds. After this duration, the cached entry
    /// is removed regardless of access. A value of 0 means no absolute expiration is applied.
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration time in seconds. The cached entry is removed if it has
    /// not been accessed within this duration. A value of 0 means no sliding expiration is applied.
    /// </summary>
    public int SlidingExpirationSeconds { get; set; }
}