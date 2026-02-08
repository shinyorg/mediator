# Shiny Mediator Code Templates

> **Important: Partial Class Requirement**
>
> When using **any middleware attribute** (`[Cache]`, `[OfflineAvailable]`, `[Resilient]`, `[MainThread]`, `[TimerRefresh]`, `[Throttle]`), the handler class **must be declared as `partial`**. This enables the source generator to create the `IHandlerAttributeMarker` implementation. Without `partial`, you'll get compiler error `SHINY001`.

## Request Handler Template

When generating a request handler, use this pattern:

```csharp
// Contract: Contracts/{Name}Request.cs
namespace {Namespace}.Contracts;

public record {Name}Request({Parameters}) : IRequest<{ResultType}>;

public record {ResultType}({ResultProperties});
```

```csharp
// Handler: Handlers/{Name}RequestHandler.cs
namespace {Namespace}.Handlers;

[Mediator{Lifetime}]  // Singleton or Scoped
// Use 'partial' when applying middleware attributes below
public partial class {Name}RequestHandler : IRequestHandler<{Name}Request, {ResultType}>
{
    private readonly {Dependencies};

    public {Name}RequestHandler({DependencyParameters})
    {
        {DependencyAssignments}
    }

    {MiddlewareAttributes}  // If any attributes used, class MUST be partial
    public async Task<{ResultType}> Handle(
        {Name}Request request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        {Implementation}
    }
}
```

**Middleware Attributes to Apply (requires `partial` class):**
- `[Cache(AbsoluteExpirationSeconds = N)]` - For cacheable queries
- `[OfflineAvailable]` - For offline-capable requests
- `[Resilient("policyName")]` - For resilience with retry/timeout
- `[MainThread]` - For MAUI UI thread execution
- `[TimerRefresh(milliseconds)]` - For auto-refresh streams
- `[Throttle(milliseconds)]` - For debouncing rapid event firings

## Command Handler Template

```csharp
// Contract: Contracts/{Name}Command.cs
namespace {Namespace}.Contracts;

public record {Name}Command({Parameters}) : ICommand;
```

```csharp
// Handler: Handlers/{Name}CommandHandler.cs
namespace {Namespace}.Handlers;

[Mediator{Lifetime}]
// Use 'partial' when applying middleware attributes
public partial class {Name}CommandHandler : ICommandHandler<{Name}Command>
{
    private readonly {Dependencies};

    public {Name}CommandHandler({DependencyParameters})
    {
        {DependencyAssignments}
    }

    {MiddlewareAttributes}  // If any attributes used, class MUST be partial
    public async Task Handle(
        {Name}Command command,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        {Implementation}
    }
}
```

## Event Handler Template

```csharp
// Contract: Contracts/{Name}Event.cs
namespace {Namespace}.Contracts;

public record {Name}Event({Parameters}) : IEvent;
```

```csharp
// Handler: Handlers/{Name}EventHandler.cs
namespace {Namespace}.Handlers;

[Mediator{Lifetime}]
// Use 'partial' when applying middleware attributes
public partial class {Name}EventHandler : IEventHandler<{Name}Event>
{
    private readonly ILogger<{Name}EventHandler> _logger;

    public {Name}EventHandler(ILogger<{Name}EventHandler> logger)
    {
        _logger = logger;
    }

    {MiddlewareAttributes}  // If any attributes used, class MUST be partial
    public Task Handle(
        {Name}Event @event,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("{EventName} received: {@Event}", nameof({Name}Event), @event);
        {Implementation}
        return Task.CompletedTask;
    }
}
```

## Stream Handler Template

```csharp
// Contract: Contracts/{Name}StreamRequest.cs
namespace {Namespace}.Contracts;

public record {Name}StreamRequest({Parameters}) : IStreamRequest<{ResultType}>;
```

```csharp
// Handler: Handlers/{Name}StreamHandler.cs
namespace {Namespace}.Handlers;

[MediatorSingleton]
// Use 'partial' when applying middleware attributes like [TimerRefresh]
public partial class {Name}StreamHandler : IStreamRequestHandler<{Name}StreamRequest, {ResultType}>
{
    {MiddlewareAttributes}  // If any attributes used (e.g., [TimerRefresh]), class MUST be partial
    public async IAsyncEnumerable<{ResultType}> Handle(
        {Name}StreamRequest request,
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return {YieldValue};
            await Task.Delay({DelayMs}, cancellationToken);
        }
    }
}
```

## Custom Middleware Template

Use `[MiddlewareOrder(n)]` to control execution order. Lower values run first (outermost in pipeline). Default is 0.

```csharp
// Middleware/{Name}Middleware.cs
namespace {Namespace}.Middleware;

[MiddlewareOrder({Order})]  // Optional: controls execution order (lower = runs first, default 0)
[MediatorSingleton]
public class {Name}Middleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    private readonly ILogger<{Name}Middleware<TRequest, TResult>> _logger;

    public {Name}Middleware(ILogger<{Name}Middleware<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Before handler execution
        _logger.LogInformation("Processing {RequestType}", typeof(TRequest).Name);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();

            // After handler execution
            stopwatch.Stop();
            _logger.LogInformation("{RequestType} completed in {ElapsedMs}ms",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

## HTTP Contract Template

```csharp
// Contracts/Http/{Name}Request.cs
namespace {Namespace}.Contracts.Http;

using Shiny.Mediator.Http;

[{HttpMethod}("{Route}")]
public class {Name}Request : IRequest<{ResultType}>
{
    // Route parameters (from URL path)
    public {RouteParamType} {RouteParamName} { get; set; }

    // Query parameters
    [Query]
    public string? {QueryParamName} { get; set; }

    // Headers
    [Header("Authorization")]
    public string? AuthToken { get; set; }

    // Body (for POST/PUT/PATCH)
    [Body]
    public {BodyType}? {BodyName} { get; set; }
}
```

## ASP.NET Endpoint Handler Template

```csharp
// Handlers/Endpoints/{Name}EndpointHandler.cs
namespace {Namespace}.Handlers.Endpoints;

[MediatorScoped]
[MediatorHttpGroup("{GroupRoute}"{AuthRequired})]
public class {Name}EndpointHandler : IRequestHandler<{Name}Request, {ResultType}>
{
    private readonly {Dependencies};

    public {Name}EndpointHandler({DependencyParameters})
    {
        {DependencyAssignments}
    }

    [MediatorHttp{Method}("{Route}"{EndpointConfig})]
    public async Task<{ResultType}> Handle(
        {Name}Request request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        {Implementation}
    }
}
```
