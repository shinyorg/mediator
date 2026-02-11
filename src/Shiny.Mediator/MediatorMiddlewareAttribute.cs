namespace Shiny.Mediator;


/// <summary>
/// Base class for all mediator middleware attributes. Apply derived attributes to handler methods
/// to configure middleware behavior. When using any derived attribute, the handler class must be
/// declared as <c>partial</c> to enable source generation of the <see cref="IHandlerAttributeMarker"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class MediatorMiddlewareAttribute : Attribute;

/// <summary>
/// Marker interface implemented by source-generated partial classes to provide AOT-safe
/// retrieval of <see cref="MediatorMiddlewareAttribute"/> instances from handler methods.
/// </summary>
public interface IHandlerAttributeMarker
{
    /// <summary>
    /// Gets the attribute of the specified type from the handler method that handles the given message.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="MediatorMiddlewareAttribute"/> to retrieve.</typeparam>
    /// <param name="message">The message object used to identify which handler method to inspect.</param>
    /// <returns>The attribute instance if found; otherwise, <c>null</c>.</returns>
    T? GetAttribute<T>(object message) where T : MediatorMiddlewareAttribute;
}