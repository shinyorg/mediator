namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Class)]
public class MediatorHttpGroupAttribute(string prefix) : Attribute
{
    public string Prefix => prefix;
    
    public bool RequiresAuthorization { get; set; }
    public string[]? AuthorizationPolicies { get; set; }
    public string? DisplayName { get; set; }
    public string? GroupName { get; set; }
    public string[]? Tags { get; set; }
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? CachePolicy { get; set; }
    public string? CorsPolicy { get; set; }
    public bool ExcludeFromDescription { get; set; }
    public string? RateLimitingPolicy { get; set; }
    public bool AllowAnonymous { get; set; }
}


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpGetAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Get);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpDeleteAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Delete);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpPostAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Post);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpPutAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Put);


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpAttribute(string uriTemplate, HttpMethod httpMethod) : Attribute
{
    public string UriTemplate => uriTemplate;
    public HttpMethod Method => httpMethod;

    public string? OperationId { get; set; }
    public bool RequiresAuthorization { get; set; }
    public string[]? AuthorizationPolicies { get; set; }
    public string? DisplayName { get; set; }
    public string? GroupName { get; set; }
    public string[]? Tags { get; set; }
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? CachePolicy { get; set; }
    public string? CorsPolicy { get; set; }
    public bool ExcludeFromDescription { get; set; }
    public string? RateLimitingPolicy { get; set; }
    public bool AllowAnonymous { get; set; }
}