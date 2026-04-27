---
name: shiny-mediator
description: Generate Shiny Mediator handlers, contracts, middleware, and scaffold projects for .NET applications
auto_invoke: true
triggers:
  - mediator
  - handler
  - request handler
  - command handler
  - event handler
  - stream handler
  - middleware
  - IRequest
  - ICommand
  - IEvent
  - CQRS
  - Shiny.Mediator
  - server sent events
  - SSE
  - EventStream
  - WaitForSingleEvent
  - IAsyncEnumerable
  - IStreamRequest
  - IServerSentEventsStream
  - OpenAPI
  - HTTP client
  - MediatorHttp
  - swagger
  - contract-first
  - strongly typed HTTP
  - AI tools
  - AITool
  - Microsoft.Extensions.AI
  - AddGeneratedAITools
  - ShinyMediatorGenerateAITools
---

# Shiny Mediator Skill

You are an expert in Shiny Mediator, a mediator pattern library for .NET applications.

## When to Use This Skill

Invoke this skill when the user wants to:
- Create request handlers, command handlers, event handlers, or stream handlers
- Generate contracts (IRequest, ICommand, IEvent, IStreamRequest)
- Add middleware (caching, resilience, validation, offline)
- Scaffold ASP.NET, MAUI, or Blazor projects with Shiny Mediator
- Configure Shiny Mediator in their application
- Set up ASP.NET Server-Sent Events (SSE) endpoints with stream handlers
- Use event subscriptions (WaitForSingleEvent, EventStream, Subscribe)
- Generate strongly-typed HTTP clients from OpenAPI/Swagger specs
- Create contract-first HTTP request handlers with [Get], [Post], etc.
- Expose mediator contracts as AI tools via Microsoft.Extensions.AI
- Migrate from MediatR to Shiny Mediator

## Library Overview

**Documentation**: https://shinylib.net/mediator

Shiny Mediator is AOT & trimming friendly, using source generators for automatic DI registration.

### Core Patterns

| Pattern | Contract | Handler | Usage |
|---------|----------|---------|-------|
| Request | `IRequest<TResult>` | `IRequestHandler<TRequest, TResult>` | Queries returning data |
| Command | `ICommand` | `ICommandHandler<TCommand>` | Void state changes |
| Event | `IEvent` | `IEventHandler<TEvent>` | Pub/sub notifications |
| Stream | `IStreamRequest<TResult>` | `IStreamRequestHandler<TRequest, TResult>` | IAsyncEnumerable |

### Handler Registration

Always use registration attributes:
```csharp
[MediatorSingleton]  // Stateless handlers
[MediatorScoped]     // Handlers needing per-request services (DbContext)
```

**Critical: Partial Class Requirement**

When using **any middleware attribute** (`[Cache]`, `[OfflineAvailable]`, `[Resilient]`, `[MainThread]`, `[TimerRefresh]`, `[Sample]`, `[Throttle]`), the handler class **must be declared as `partial`**:
```csharp
[MediatorSingleton]
public partial class MyHandler : IRequestHandler<MyRequest, MyResult>  // partial required!
{
    [Cache(AbsoluteExpirationSeconds = 60)]
    public Task<MyResult> Handle(...) { }
}
```
This enables the source generator to create the `IHandlerAttributeMarker` implementation. Without `partial`, you'll get error `SHINY001`.

### Basic Setup

**ASP.NET:**
```csharp
builder.Services.AddShinyMediator(x => x
    .AddMediatorRegistry()
);
app.MapGeneratedMediatorEndpoints();
```

**MAUI:**
```csharp
builder.AddShinyMediator(x => x
    .AddMediatorRegistry()
    .UseMaui()
    .AddMauiPersistentCache()
    .PreventEventExceptions()
);
```

**Blazor:**
```csharp
builder.Services.AddShinyMediator(x => x
    .AddMediatorRegistry()
    .UseBlazor()
    .PreventEventExceptions()
);
```

## Code Generation Instructions

When generating Shiny Mediator code:

### 1. Contracts

Always use records for immutability:
```csharp
public record GetUserRequest(int UserId) : IRequest<UserDto>;
public record CreateUserCommand(string Name, string Email) : ICommand;
public record UserCreatedEvent(int UserId, string Name) : IEvent;
```

### 2. Handlers

Include all three parameters in Handle method:
```csharp
[MediatorScoped]
public class GetUserRequestHandler : IRequestHandler<GetUserRequest, UserDto>
{
    public Task<UserDto> Handle(
        GetUserRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### 3. Middleware Attributes

Apply to handler methods as needed:
- `[Cache(AbsoluteExpirationSeconds = N)]` - Cacheable queries
- `[OfflineAvailable]` - Offline storage for mobile
- `[Resilient("policyName")]` - Retry/timeout policies
- `[MainThread]` - MAUI main thread execution
- `[TimerRefresh(milliseconds)]` - Auto-refresh streams
- `[Sample(milliseconds)]` - Fixed-window sampling (last event in window executes)
- `[Throttle(milliseconds)]` - True throttle (first event executes, cooldown discards rest)
- `[Validate]` - Data annotation validation

**When using ANY of these attributes, the handler class MUST be `partial`:**
```csharp
[MediatorSingleton]
public partial class CachedHandler : IRequestHandler<MyRequest, MyData>
{
    [Cache(AbsoluteExpirationSeconds = 60)]
    [OfflineAvailable]
    public Task<MyData> Handle(...) { }
}
```

### 4. Middleware Ordering

Use `[MiddlewareOrder(int)]` on custom middleware classes to control execution order. Lower values run first (outermost). Default is 0.
```csharp
[MiddlewareOrder(-100)]  // Runs before middleware with higher order values
[MediatorSingleton]
public class EarlyMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{ ... }
```

### 5. File Organization

Place files in appropriate folders:
- Contracts: `Contracts/{Name}Request.cs`, `Contracts/{Name}Command.cs`
- Handlers: `Handlers/{Name}Handler.cs`
- Middleware: `Middleware/{Name}Middleware.cs`

## Usage Examples

**Request:**
```csharp
var response = await mediator.Request(new GetUserRequest(1));
var user = response.Result;
```

**Command:**
```csharp
await mediator.Send(new CreateUserCommand("John", "john@example.com"));
```

**Event:**
```csharp
await mediator.Publish(new UserCreatedEvent(1, "John"));
```

**Chaining via Context:**
```csharp
public async Task<UserDto> Handle(GetUserRequest request, IMediatorContext context, CancellationToken ct)
{
    // Use context to chain operations (shares scope)
    await context.Publish(new UserAccessedEvent(request.UserId));
    return new UserDto(...);
}
```

### Event Subscriptions & Streaming

**WaitForSingleEvent** - Await a single event occurrence (with optional filter):
```csharp
// Wait for a specific event (blocks until event fires or cancellation)
var evt = await mediator.WaitForSingleEvent<OrderCompletedEvent>(
    filter: e => e.OrderId == orderId,
    cancellationToken: ct
);
```

**EventStream** - Continuous IAsyncEnumerable stream of events (uses Channels internally):
```csharp
// Consume events as an async stream
await foreach (var evt in mediator.EventStream<PriceUpdatedEvent>(cancellationToken: ct))
{
    Console.WriteLine($"New price: {evt.Price}");
}
```

**Subscribe** - Manual subscription returning IDisposable:
```csharp
var sub = mediator.Subscribe<MyEvent>((ev, ctx, ct) =>
{
    Console.WriteLine($"Event received: {ev}");
    return Task.CompletedTask;
});
// Later: sub.Dispose() to unsubscribe
```

### ASP.NET Server-Sent Events (SSE)

Stream handlers decorated with `[MediatorHttpGet]` or `[MediatorHttpPost]` on an `IStreamRequestHandler` are **automatically generated as SSE endpoints** by the source generator via `MapGeneratedMediatorEndpoints()`.

**Manual SSE endpoint with EventStream:**
```csharp
app.MapGet("/events", ([FromServices] IMediator mediator) =>
    TypedResults.ServerSentEvents(mediator.EventStream<MyEvent>())
);
```

**Stream handler as auto-generated SSE endpoint:**
```csharp
public record TickerStreamRequest : IStreamRequest<int>;

[MediatorScoped]
public class TickerStreamHandler : IStreamRequestHandler<TickerStreamRequest, int>
{
    [MediatorHttpGet("/ticker")]
    public async IAsyncEnumerable<int> Handle(
        TickerStreamRequest request,
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var i = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return i++;
            await Task.Delay(1000, cancellationToken);
        }
    }
}
```

**HTTP client-side SSE consumption:** Implement `IServerSentEventsStream` marker on the contract to indicate the server returns SSE format. The generated HTTP handler will use `ReadServerSentEvents<T>()` to parse the `data:` prefixed SSE lines.
```csharp
public record TickerStreamRequest : IStreamRequest<int>, IServerSentEventsStream;
```

## Contract-First HTTP Clients

Shiny Mediator generates strongly-typed HTTP client handlers from contract classes decorated with HTTP method attributes. No manual `HttpClient` code needed.

### Manual HTTP Contracts

Decorate request classes with `[Get]`, `[Post]`, `[Put]`, `[Delete]`, `[Patch]` and use `[Query]`, `[Header]`, `[Body]` on properties:
```csharp
[Get("/api/orders/{OrderId}")]
public class GetOrderRequest : IRequest<OrderDto>
{
    public int OrderId { get; set; }          // Route parameter (matches {OrderId})

    [Query("status")]
    public string? Status { get; set; }        // ?status=value

    [Header("Authorization")]
    public string? AuthToken { get; set; }     // HTTP header
}

[Post("/api/orders")]
public class CreateOrderRequest : IRequest<OrderDto>
{
    [Body]
    public CreateOrderBody? Body { get; set; } // JSON request body
}
```

The source generator creates handler classes inheriting `BaseHttpRequestHandler` that build routes, add query/header parameters, serialize bodies, and call `IHttpClientFactory`.

**Registration:**
```csharp
builder.Services.AddShinyMediator(x => x
    .AddMediatorRegistry()
    .AddStrongTypedHttpClient()   // Registers generated HTTP handlers
);
```

### OpenAPI Client Generation

Generate contracts, models, and handlers directly from OpenAPI/Swagger specs. Add a `<MediatorHttp>` item in your `.csproj`:

```xml
<ItemGroup>
    <!-- Remote URL: Include is a logical name, Uri points to the spec -->
    <MediatorHttp Include="MyApi"
                  Uri="https://api.example.com/swagger/v1/swagger.json"
                  Namespace="MyApp.ExternalApi"
                  ContractPostfix="HttpRequest"
                  GenerateJsonConverters="true"
                  Visible="false" />

    <!-- Local file: Include is the file path, no Uri needed -->
    <MediatorHttp Include="./specs/openapi.yaml"
                  Namespace="MyApp.LocalApi"
                  Visible="false" />
</ItemGroup>
```

`Include` can be a **file path** (for local specs) or a **logical name** when `Uri` is set separately.

**MediatorHttp metadata options:**

| Metadata | Description |
|----------|-------------|
| `Uri` | URL or local path to OpenAPI JSON/YAML spec |
| `Namespace` | C# namespace for generated types |
| `ContractPrefix` | Prefix for generated contract class names |
| `ContractPostfix` | Postfix for generated contract class names |
| `UseInternalClasses` | Generate internal classes instead of public |
| `GenerateModelsOnly` | Only generate models, no handlers |
| `GenerateJsonConverters` | Generate `JsonConverter` implementations for enums |

**Registration of generated OpenAPI client:**
```csharp
builder.Services.AddShinyMediator(x => x
    .AddMediatorRegistry()
    .AddGeneratedOpenApiClient()   // Registers all OpenAPI-generated handlers
);
```

Then use like any other mediator request:
```csharp
var result = await mediator.Request(new GetPetsHttpRequest { Status = "available" });
```

## AI Tools Integration (Microsoft.Extensions.AI)

Shiny Mediator can expose your request and command contracts as AI-callable tools via `Microsoft.Extensions.AI`. The source generator discovers contracts annotated with `[Description]` and generates `AIFunction` wrappers with JSON schemas automatically.

### Prerequisites

1. Install the `Microsoft.Extensions.AI` NuGet package:
```bash
dotnet add package Microsoft.Extensions.AI
```

2. Enable AI tool generation in your `.csproj`:
```xml
<PropertyGroup>
    <ShinyMediatorGenerateAITools>true</ShinyMediatorGenerateAITools>
</PropertyGroup>
```

:::caution
If `ShinyMediatorGenerateAITools` is enabled but `Microsoft.Extensions.AI` is not referenced, the source generator emits compiler error **SHINYMED100**.
:::

### Annotating Contracts

Add `[Description]` to your contracts and their properties to make them discoverable by AI:

```csharp
using System.ComponentModel;

[Description("Gets the weather forecast for a given city")]
public record GetWeatherRequest(
    [property: Description("The city name to get weather for")]
    string City
) : IRequest<WeatherResult>;

[Description("Performs a mathematical calculation")]
public record CalculateRequest(
    [property: Description("First operand")]
    double A,
    [property: Description("Math operator: +, -, *, /")]
    string Operator,
    [property: Description("Second operand")]
    double B
) : IRequest<double>;

[Description("Sends a notification to a user")]
public record SendNotificationCommand(
    [property: Description("The user ID to notify")]
    int UserId,
    [property: Description("The notification message")]
    string Message
) : ICommand;
```

### Registration

Register the generated AI tools with the mediator builder:

```csharp
builder.Services.AddShinyMediator(cfg =>
{
    cfg.AddMediatorRegistry();
    cfg.AddGeneratedAITools(); // Registers all [Description]-annotated contracts as AITool instances
});
```

### Using with a Chat Client

Retrieve the tools from DI and pass them to any `IChatClient`:

```csharp
using Microsoft.Extensions.AI;

var tools = host.Services.GetServices<AITool>().ToList();
var options = new ChatOptions { Tools = tools };
var response = await chatClient.GetResponseAsync(history, options);
```

### What Gets Generated

For each contract with `[Description]` that implements `IRequest<T>` or `ICommand`, the source generator produces:

1. **An `AIFunction` class** — wraps the mediator call with a JSON schema built from the contract's properties
2. **A registration extension method** — `AddGeneratedAITools()` on `ShinyMediatorBuilder` that registers all generated tools as `AITool` singletons

The generated JSON schema includes property types, descriptions, required fields, enum values, and default values.

### Supported Property Types

| Type | JSON Schema Type |
|:-----|:----------------|
| `string`, `Guid`, `Uri`, `DateTime`, `DateTimeOffset`, `DateOnly` | `string` |
| `bool` | `boolean` |
| `int`, `long`, `short`, `byte` | `integer` |
| `float`, `double`, `decimal` | `number` |
| Enums | `string` with `enum` values |
| Arrays / `IEnumerable<T>` | `array` |
| Complex types | `object` |

## Best Practices

1. **Use records for contracts** - Immutable, value equality
2. **One handler per file** - Focused and testable
3. **Appropriate lifetime** - Singleton for stateless, Scoped for DbContext
4. **Use `partial` with middleware attributes** - Required for `[Cache]`, `[OfflineAvailable]`, `[Resilient]`, etc.
5. **Chain via context** - Use `context.Request()` not injecting IMediator
6. **Implement IContractKey** - For custom cache/offline keys
7. **Always pass CancellationToken** - Respect cancellation
8. **Use IServerSentEventsStream marker** - On stream contracts consumed via HTTP SSE
9. **Stream handlers only support GET/POST** - Other HTTP methods are not valid for SSE endpoints
10. **Use EventStream for SSE push endpoints** - Combine `mediator.EventStream<T>()` with `TypedResults.ServerSentEvents()` for event-driven SSE

## Reference Files

For detailed templates and examples, see:
- `reference/templates.md` - Code generation templates (includes SSE endpoint templates)
- `reference/scaffolding.md` - Project structure templates
- `reference/middleware.md` - Middleware configuration
- `reference/api-reference.md` - Full API, event subscriptions, SSE, and NuGet packages

## Common Packages

```bash
dotnet add package Shiny.Mediator                              # Core
dotnet add package Shiny.Mediator.Maui                         # MAUI
dotnet add package Shiny.Mediator.Blazor                       # Blazor
dotnet add package Shiny.Mediator.AspNet                       # ASP.NET
dotnet add package Shiny.Mediator.Resilience                   # Polly
dotnet add package Shiny.Mediator.Caching.MicrosoftMemoryCache # Caching
dotnet add package Shiny.Mediator.AppSupport                   # Offline
dotnet add package Microsoft.Extensions.AI                     # AI Tools
```
