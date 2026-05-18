namespace Shiny.Mediator;


/// <summary>
/// Marks a class as the source of a route group whose endpoints are generated from member-level
/// <see cref="MediatorHttpAttribute"/> instances. Properties on this attribute apply to every endpoint in the
/// group (authorization, policies, OpenAPI metadata, etc.).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MediatorHttpGroupAttribute(string prefix) : Attribute
{
    /// <summary>The route prefix shared by every endpoint in the group.</summary>
    public string Prefix => prefix;

    /// <summary>When <c>true</c>, applies <c>RequireAuthorization</c> to the generated group.</summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>Named authorization policies enforced for every endpoint in the group.</summary>
    public string[]? AuthorizationPolicies { get; set; }

    /// <summary>Display name applied via <c>WithDisplayName</c>.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Group name applied via <c>WithGroupName</c>.</summary>
    public string? GroupName { get; set; }

    /// <summary>OpenAPI/Swagger tags applied via <c>WithTags</c>.</summary>
    public string[]? Tags { get; set; }

    /// <summary>OpenAPI description applied via <c>WithDescription</c>.</summary>
    public string? Description { get; set; }

    /// <summary>OpenAPI summary applied via <c>WithSummary</c>.</summary>
    public string? Summary { get; set; }

    /// <summary>Output cache policy applied via <c>CacheOutput</c>.</summary>
    public string? CachePolicy { get; set; }

    /// <summary>Named CORS policy applied via <c>RequireCors</c>.</summary>
    public string? CorsPolicy { get; set; }

    /// <summary>When <c>true</c>, hides the group from API description providers.</summary>
    public bool ExcludeFromDescription { get; set; }

    /// <summary>Named rate-limiting policy applied via <c>RequireRateLimiting</c>.</summary>
    public string? RateLimitingPolicy { get; set; }

    /// <summary>When <c>true</c>, applies <c>AllowAnonymous</c> to the group.</summary>
    public bool AllowAnonymous { get; set; }
}


/// <summary>
/// Maps the decorated handler method to an HTTP GET endpoint at the supplied URI template.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpGetAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Get);

/// <summary>
/// Maps the decorated handler method to an HTTP DELETE endpoint at the supplied URI template.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpDeleteAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Delete);

/// <summary>
/// Maps the decorated handler method to an HTTP POST endpoint at the supplied URI template.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpPostAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Post);

/// <summary>
/// Maps the decorated handler method to an HTTP PUT endpoint at the supplied URI template.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpPutAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Put);


/// <summary>
/// Base attribute that maps a mediator handler method to an HTTP endpoint with the given URI template and HTTP
/// method. Use the verb-specific subclasses (<see cref="MediatorHttpGetAttribute"/>, etc.) in source. Properties
/// configure per-endpoint authorization, OpenAPI metadata, and ASP.NET policy attachments.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpAttribute(string uriTemplate, HttpMethod httpMethod) : Attribute
{
    /// <summary>The URI template for the generated endpoint.</summary>
    public string UriTemplate => uriTemplate;

    /// <summary>The HTTP verb the endpoint responds to.</summary>
    public HttpMethod Method => httpMethod;

    /// <summary>OpenAPI operation id applied via <c>WithName</c>.</summary>
    public string? OperationId { get; set; }

    /// <summary>When <c>true</c>, applies <c>RequireAuthorization</c> to the endpoint.</summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>Named authorization policies enforced for the endpoint.</summary>
    public string[]? AuthorizationPolicies { get; set; }

    /// <summary>Display name applied via <c>WithDisplayName</c>.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Group name applied via <c>WithGroupName</c>.</summary>
    public string? GroupName { get; set; }

    /// <summary>OpenAPI/Swagger tags applied via <c>WithTags</c>.</summary>
    public string[]? Tags { get; set; }

    /// <summary>OpenAPI description applied via <c>WithDescription</c>.</summary>
    public string? Description { get; set; }

    /// <summary>OpenAPI summary applied via <c>WithSummary</c>.</summary>
    public string? Summary { get; set; }

    /// <summary>Output cache policy applied via <c>CacheOutput</c>.</summary>
    public string? CachePolicy { get; set; }

    /// <summary>Named CORS policy applied via <c>RequireCors</c>.</summary>
    public string? CorsPolicy { get; set; }

    /// <summary>When <c>true</c>, hides the endpoint from API description providers.</summary>
    public bool ExcludeFromDescription { get; set; }

    /// <summary>Named rate-limiting policy applied via <c>RequireRateLimiting</c>.</summary>
    public string? RateLimitingPolicy { get; set; }

    /// <summary>When <c>true</c>, applies <c>AllowAnonymous</c> to the endpoint.</summary>
    public bool AllowAnonymous { get; set; }
}