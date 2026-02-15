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

When using **any middleware attribute** (`[Cache]`, `[OfflineAvailable]`, `[Resilient]`, `[MainThread]`, `[TimerRefresh]`, `[Throttle]`), the handler class **must be declared as `partial`**:
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
- `[Throttle(milliseconds)]` - Debounce rapid event firings
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
```
