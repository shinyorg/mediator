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
    public bool UseOpenApi { get; set; } = true;
    public string? CachePolicy { get; set; }
    public string? CorsPolicy { get; set; }
    public bool ExcludeFromDescription { get; set; }
    public string? RateLimitingPolicy { get; set; }
    public bool AllowAnonymous { get; set; }
}


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpGetAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Get);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpDeleteAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Delete);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpPostAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Post);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpPutAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Put);


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MediatorHttpAttribute(string operationId, string uriTemplate, HttpMethod httpMethod) : Attribute
{
    public string UriTemplate => uriTemplate;
    public HttpMethod Method => httpMethod;

    public string OperationId => operationId;
    public bool RequiresAuthorization { get; set; }
    public string[]? AuthorizationPolicies { get; set; }
    public string? DisplayName { get; set; }
    public string? GroupName { get; set; }
    public string[]? Tags { get; set; }
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public bool UseOpenApi { get; set; } = true;
    public string? CachePolicy { get; set; }
    public string? CorsPolicy { get; set; }
    public bool ExcludeFromDescription { get; set; }
    public string? RateLimitingPolicy { get; set; }
    public bool AllowAnonymous { get; set; }
}