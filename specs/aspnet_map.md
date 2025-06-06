Given the following C# attributes

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpGetAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Get);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpDeleteAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Delete);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpPostAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Post);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpPutAttribute(string operationId, string uriTemplate) : MediatorHttpAttribute(operationId, uriTemplate, HttpMethod.Put);


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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

```

and given the following samples

```csharp
public record GetUserRequest(int Id) : IRequest<Response>;

public record Response(string Name, string Email);


[MediatorHttpGet(
    "GetUser", 
    "/users/{id}"
)]
public class GetUserRequestHandler : IRequestHandler<AddScopedAsImplementedInterfaces>
{
    public Task<Response> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        // Simulate fetching user data
        return Task.FromResult(new Response("John Doe", "Something");
    }
}
```

I want to create a C# incremental source generator that creates the following the following based on classes marked with the attributes.  

* Attributes can only be marked on the request (Shiny.Mediator.) or command handlers.
* Source generator should use fully qualified names for all types that in the generated code
* It will generate two separate files
    * Dependency Injection Map
    * ASP.NET minimal route methods


## Dependency Injection Map Sample Output

```csharp

public static class MediatorDependencyInjectionExtensions
{
    public static ShinyMediatorBuilder AddMediatorGeneratedEndpoints(this ShinyMediatorBuilder builder)
    {
        builder.Services.AddScopedAsImplementedInterfaces<GetUserRequestHandler>();
        return builder;
    }
}

```


## ASPNET Minimal Route Methods Sample Output
It will also generate the following ASP.NET minimal route methods.  
* Command handlers do not return a response.  500 if exception, 200 if sucessful.
* Request handlers return a response. 500 if exception, 200 if successful.

```csharp

public static class MediatorEndpoints 
{
    public static IEndpointRouteBuilder MapMediatorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            // Map type based on attribute
            // URL with parameters should map into the arguments of the minimal route method
            .MapGet(
                "/users/{id}", 
                async (
                    [FromServices] IMediator mediator,
                    [AsParameters] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    return Results.Ok(result);
                }
            )
            // Operation ID is required for OpenAPI documentation
            .WithName("GetUser")

            // if DisplayName is set, use it
            .WithDisplayName("Get User by ID")
            
            // if Tags is set, put each one here
            .WithTags("Users")

            // if RequiresAuthorization is true, add authorization
            .RequireAuthorization()

            // If UseOpenApi is true, add OpenAPI support
            .WithOpenApi();

            // If CachePolicy is set, apply caching
            // If CorsPolicy is set, apply CORS policy
            // If RateLimitingPolicy is set, apply rate limiting
            // If ExcludeFromDescription is true, do not include in OpenAPI description

        return endpoints;
    }
}
```