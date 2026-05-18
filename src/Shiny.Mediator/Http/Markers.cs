namespace Shiny.Mediator.Http;


/// <summary>
/// Decorator hook applied to outgoing HTTP requests issued by source-generated mediator HTTP handlers.
/// Implementations can mutate headers, attach auth tokens, log, or otherwise observe the request.
/// </summary>
public interface IHttpRequestDecorator
{
    /// <summary>
    /// Mutates or inspects <paramref name="httpMessage"/> before it is sent.
    /// </summary>
    Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, CancellationToken cancellationToken);
}


/// <summary>
/// Marker interface implemented by stream request contracts whose HTTP responses are formatted as
/// Server-Sent Events (SSE) rather than a streamed JSON array.
/// </summary>
public interface IServerSentEventsStream;

/// <summary>
/// Marks a request/command contract as an HTTP <c>GET</c> against the given relative or absolute route.
/// Consumed by the mediator HTTP source generator.
/// </summary>
/// <param name="route">Route template applied to the configured base URI.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class GetAttribute(string route) : Attribute;

/// <summary>
/// Marks a request/command contract as an HTTP <c>DELETE</c> against the given relative or absolute route.
/// Consumed by the mediator HTTP source generator.
/// </summary>
/// <param name="route">Route template applied to the configured base URI.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DeleteAttribute(string route) : Attribute;

/// <summary>
/// Marks a request/command contract as an HTTP <c>PATCH</c> against the given relative or absolute route.
/// Consumed by the mediator HTTP source generator.
/// </summary>
/// <param name="route">Route template applied to the configured base URI.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PatchAttribute(string route) : Attribute;

/// <summary>
/// Marks a request/command contract as an HTTP <c>POST</c> against the given relative or absolute route.
/// Consumed by the mediator HTTP source generator.
/// </summary>
/// <param name="route">Route template applied to the configured base URI.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PostAttribute(string route) : Attribute;

/// <summary>
/// Marks a request/command contract as an HTTP <c>PUT</c> against the given relative or absolute route.
/// Consumed by the mediator HTTP source generator.
/// </summary>
/// <param name="route">Route template applied to the configured base URI.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PutAttribute(string route) : Attribute;



/// <summary>
/// Marks a property on an HTTP contract as a request header. Defaults to using the property name unless <paramref name="Name"/> is supplied.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class HeaderAttribute(string? Name = null) : Attribute;

/// <summary>
/// Marks a property on an HTTP contract as the request body. Only one body property is permitted per contract.
/// </summary>
/// <param name="Serialization">Serialization strategy applied to the body value.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class BodyAttribute(BodySerialization Serialization = BodySerialization.Json) : Attribute;

/// <summary>
/// Marks a property on an HTTP contract as a URL query-string parameter. Defaults to using the property name unless <paramref name="Name"/> is supplied.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class QueryAttribute(string? Name = null) : Attribute;

/// <summary>
/// Body serialization formats supported by HTTP contract properties marked with <see cref="BodyAttribute"/>.
/// </summary>
public enum BodySerialization
{
    /// <summary>Body is serialized as JSON.</summary>
    Json,
    /// <summary>Body is serialized as <c>application/x-www-form-urlencoded</c>.</summary>
    FormUrlEncoded
    // MultipartFormData
}
