# Middleware Reference

> **Critical: Partial Class Requirement**
>
> When using **any middleware attribute** on a handler method, the handler class **must be declared as `partial`**. This enables the source generator to create the `IHandlerAttributeMarker` implementation for runtime attribute lookup.
>
> **Without `partial`, you'll get compiler error `SHINY001`.**

## Built-in Middleware Attributes

Apply middleware to handlers using attributes. **Remember: class must be `partial`!**

```csharp
[MediatorSingleton]
public partial class MyRequestHandler : IRequestHandler<MyRequest, string>  // MUST be partial!
{
    [Cache(AbsoluteExpirationSeconds = 300)]  // Cache for 5 minutes
    [OfflineAvailable]                         // Store for offline access
    [Resilient("default")]                     // Apply resilience policy
    [MainThread]                               // Execute on main thread (MAUI)
    [TimerRefresh(5000)]                       // Auto-refresh stream every 5s
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
    {
        return Task.FromResult("result");
    }
}
```

## Caching Middleware

```csharp
// Setup
builder.AddShinyMediator(x => x
    .AddMauiPersistentCache()  // or .AddMemoryCaching()
);

// Handler - MUST be partial when using [Cache]
[MediatorSingleton]
public partial class CachedHandler : IRequestHandler<MyRequest, MyData>
{
    [Cache(AbsoluteExpirationSeconds = 60, SlidingExpirationSeconds = 30)]
    public Task<MyData> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
    {
        return FetchFromApi();
    }
}

// Check if result came from cache
var response = await mediator.Request(new MyRequest());
var cacheInfo = response.Context.Cache();  // Returns CacheContext if cached
```

## Offline Availability (App Support)

```csharp
// Setup
builder.AddShinyMediator(x => x
    .AddStandardAppSupportMiddleware()  // Includes offline, replay, user notifications
);

// Handler
[MediatorSingleton]
public partial class OfflineHandler : IRequestHandler<MyRequest, MyData>
{
    [OfflineAvailable]
    public Task<MyData> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
    {
        return FetchFromApi();
    }
}

// Check if result came from offline storage
var response = await mediator.Request(new MyRequest());
var offlineInfo = response.Context.Offline();  // Returns OfflineAvailableContext if offline
```

## Resilience Middleware (Polly)

```csharp
// Setup via configuration
builder.AddShinyMediator(x => x
    .AddResiliencyMiddleware(builder.Configuration)
);

// appsettings.json
{
    "Resilience": {
        "default": {
            "TimeoutMilliseconds": 5000,
            "Retry": {
                "MaxAttempts": 3,
                "DelayMilliseconds": 1000,
                "BackoffType": "Exponential",
                "UseJitter": true
            }
        }
    }
}

// Handler - MUST be partial when using [Resilient]
[MediatorSingleton]
public partial class ResilientHandler : IRequestHandler<MyRequest, string>
{
    [Resilient("default")]
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
    {
        return CallUnreliableApi();
    }
}

// Or setup programmatically
builder.AddShinyMediator(x => x
    .AddResiliencyMiddleware(
        ("myPolicy", builder => builder
            .AddTimeout(TimeSpan.FromSeconds(10))
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 }))
    )
);
```

## Validation Middleware

```csharp
// Data Annotations
builder.AddShinyMediator(x => x.AddDataAnnotations());

// Contract with validation
[Validate]
public class CreateUserCommand : ICommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(2)]
    public string Name { get; set; }
}

// FluentValidation
builder.AddShinyMediator(x => x.AddFluentValidation());
```

## Event Throttling

Throttle rapid event firings so only the last event in a window is processed (debounce pattern).

```csharp
// Setup
builder.AddShinyMediator(x => x
    .AddThrottleEventMiddleware()
);

// Handler - MUST be partial when using [Throttle]
[MediatorSingleton]
public partial class SearchHandler : IEventHandler<SearchChangedEvent>
{
    [Throttle(500)]  // 500ms debounce window
    public Task Handle(SearchChangedEvent @event, IMediatorContext context, CancellationToken ct)
    {
        // Only executes after 500ms of no new events
        return PerformSearch(@event.Query);
    }
}
```

## Performance Logging

```csharp
builder.AddShinyMediator(x => x.AddPerformanceLoggingMiddleware());
```

## Middleware Ordering

Control middleware execution order with `[MiddlewareOrder]` on middleware classes. Lower values run first (outermost in pipeline). Default order is 0.

```csharp
[MiddlewareOrder(-100)]  // Runs first - validation before anything else
[MediatorSingleton]
public class ValidationMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        Validate(context.Message);
        return await next();
    }
}

[MiddlewareOrder(100)]  // Runs last - closest to handler
[MediatorSingleton]
public class CachingMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Check cache, call next() on miss
        return await next();
    }
}
```

Middleware without `[MiddlewareOrder]` defaults to 0. Middleware with the same order preserves DI registration order (stable sort).

## Custom Middleware

```csharp
// Request middleware
[MediatorSingleton]
public class LoggingMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Before: {typeof(TRequest).Name}");
        var result = await next();
        Console.WriteLine($"After: {typeof(TRequest).Name}");
        return result;
    }
}

// Register open generic middleware
builder.AddShinyMediator(x => x
    .AddOpenRequestMiddleware(typeof(LoggingMiddleware<,>))
);
```

## Exception Handling

```csharp
// Global exception handler
public class MyExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        // Return true if handled, false to rethrow
        if (exception is MyExpectedException)
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}

// Register
builder.AddShinyMediator(x => x
    .AddExceptionHandler<MyExceptionHandler>()
    .PreventEventExceptions()  // Prevents event handler exceptions from crashing
);

// ASP.NET JSON validation response
builder.AddShinyMediator(x => x.AddJsonValidationExceptionHandler());
```
