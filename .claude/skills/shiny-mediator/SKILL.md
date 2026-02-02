# Shiny Mediator - Claude Skill

## Overview

Shiny Mediator is a mediator pattern implementation for ALL .NET applications. It provides request/response handling, event publication, async enumerable streams, and powerful middleware capabilities. The library is fully AOT & trimming friendly, using source generators for automatic registration.

**Documentation**: https://shinylib.net/mediator

**Key Features:**
- Request/Response pattern with `IRequest<TResult>` and `IRequestHandler<TRequest, TResult>`
- Commands with `ICommand` and `ICommandHandler<TCommand>`
- Events with `IEvent` and `IEventHandler<TEvent>`
- Async enumerable streams with `IStreamRequest<TResult>` and `IStreamRequestHandler<TRequest, TResult>`
- Rich middleware pipeline for cross-cutting concerns
- Source generators for automatic DI registration
- Works with MAUI, Blazor, ASP.NET, Uno Platform, and any .NET application

## Installation

```bash
# Core package
dotnet add package Shiny.Mediator

# Platform-specific packages
dotnet add package Shiny.Mediator.Maui          # For .NET MAUI
dotnet add package Shiny.Mediator.Blazor        # For Blazor
dotnet add package Shiny.Mediator.AspNet        # For ASP.NET
dotnet add package Shiny.Mediator.Uno           # For Uno Platform

# Middleware packages
dotnet add package Shiny.Mediator.Resilience    # Polly resilience
dotnet add package Shiny.Mediator.FluentValidation
dotnet add package Shiny.Mediator.Caching.MicrosoftMemoryCache
dotnet add package Shiny.Mediator.AppSupport    # Offline, replay, user notifications
```

## Basic Setup

### ASP.NET / Console Applications

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator(x => x
    .AddMediatorRegistry()  // Source-generated registry
);

var app = builder.Build();
app.MapGeneratedMediatorEndpoints(); // For ASP.NET HTTP endpoint mapping
app.Run();
```

### .NET MAUI

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    builder.AddShinyMediator(x => x
        .AddMediatorRegistry()
        .UseMaui()
        .UseBlazor()  // If using Blazor hybrid
        .PreventEventExceptions()
        .AddDataAnnotations()
        .AddMauiPersistentCache()
    );
    
    return builder.Build();
}
```

## Core Concepts

### 1. Requests (Query Pattern)

Requests return a result. Use for queries or operations that return data.

**Contract:**
```csharp
// Define the request
public record GetUserRequest(int UserId) : IRequest<UserDto>;

// Define the response
public record UserDto(int Id, string Name, string Email);
```

**Handler:**
```csharp
[MediatorScoped]  // or [MediatorSingleton]
public class GetUserRequestHandler : IRequestHandler<GetUserRequest, UserDto>
{
    public Task<UserDto> Handle(
        GetUserRequest request, 
        IMediatorContext context, 
        CancellationToken cancellationToken)
    {
        // Fetch user from database
        return Task.FromResult(new UserDto(request.UserId, "John", "john@example.com"));
    }
}
```

**Usage:**
```csharp
public class MyService(IMediator mediator)
{
    public async Task DoWork()
    {
        var response = await mediator.Request(new GetUserRequest(1));
        var user = response.Result;
        var context = response.Context; // Access middleware metadata
    }
}
```

### 2. Commands (Void Operations)

Commands don't return data. Use for operations that change state.

**Contract:**
```csharp
public record CreateUserCommand(string Name, string Email) : ICommand;
```

**Handler:**
```csharp
[MediatorScoped]
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public Task Handle(
        CreateUserCommand command, 
        IMediatorContext context, 
        CancellationToken cancellationToken)
    {
        // Create user in database
        return Task.CompletedTask;
    }
}
```

**Usage:**
```csharp
await mediator.Send(new CreateUserCommand("John", "john@example.com"));
```

### 3. Events (Pub/Sub)

Events notify multiple handlers. Handlers can be registered via DI or implement `IEventHandler<T>` on ViewModels/Pages.

**Contract:**
```csharp
public record UserCreatedEvent(int UserId, string Name) : IEvent;
```

**Handler (Registered via DI):**
```csharp
[MediatorSingleton]
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public Task Handle(
        UserCreatedEvent @event, 
        IMediatorContext context, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"User created: {@event.Name}");
        return Task.CompletedTask;
    }
}
```

**Handler (ViewModel - MAUI/Blazor):**
```csharp
// ViewModels can implement IEventHandler directly - no DI registration needed
public partial class MyViewModel : ObservableObject, IEventHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent @event, IMediatorContext context, CancellationToken ct)
    {
        // Update UI state
        return Task.CompletedTask;
    }
}
```

**Publishing Events:**
```csharp
// Synchronous publish (waits for all handlers)
await mediator.Publish(new UserCreatedEvent(1, "John"));

// Fire and forget (background)
mediator.PublishToBackground(new UserCreatedEvent(1, "John"));

// Parallel vs Sequential execution
await mediator.Publish(new UserCreatedEvent(1, "John"), executeInParallel: true);
```

**Runtime Event Subscription:**
```csharp
// Subscribe dynamically
using var subscription = mediator.Subscribe<UserCreatedEvent>((ev, ctx, ct) =>
{
    Console.WriteLine($"User: {ev.Name}");
    return Task.CompletedTask;
});

// Wait for a single event
var @event = await mediator.WaitForSingleEvent<UserCreatedEvent>();

// Stream events
await foreach (var ev in mediator.EventStream<UserCreatedEvent>())
{
    Console.WriteLine(ev.Name);
}
```

### 4. Stream Requests (IAsyncEnumerable)

For returning multiple values over time.

**Contract:**
```csharp
public record TickerRequest(string Symbol, int IntervalSeconds) : IStreamRequest<decimal>;
```

**Handler:**
```csharp
[MediatorSingleton]
public class TickerStreamHandler : IStreamRequestHandler<TickerRequest, decimal>
{
    public async IAsyncEnumerable<decimal> Handle(
        TickerRequest request,
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return GetCurrentPrice(request.Symbol);
            await Task.Delay(TimeSpan.FromSeconds(request.IntervalSeconds), cancellationToken);
        }
    }
}
```

**Usage:**
```csharp
await foreach (var (context, price) in mediator.Request(new TickerRequest("MSFT", 5)))
{
    Console.WriteLine($"Price: {price}");
}
```

## Handler Registration Attributes

Use source-generated attributes for automatic DI registration:

```csharp
[MediatorSingleton]  // Registered as singleton
public class MySingletonHandler : IRequestHandler<MyRequest, string> { }

[MediatorScoped]     // Registered as scoped (per-request)
public class MyScopedHandler : ICommandHandler<MyCommand> { }
```

## Middleware

### Built-in Middleware Attributes

Apply middleware to handlers using attributes:

```csharp
[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
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

### Caching Middleware

```csharp
// Setup
builder.AddShinyMediator(x => x
    .AddMauiPersistentCache()  // or .AddMemoryCaching()
);

// Handler
[MediatorSingleton]
public class CachedHandler : IRequestHandler<MyRequest, MyData>
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

### Offline Availability (App Support)

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

### Resilience Middleware (Polly)

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

// Handler
[MediatorSingleton]
public class ResilientHandler : IRequestHandler<MyRequest, string>
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

### Validation Middleware

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

### Performance Logging

```csharp
builder.AddShinyMediator(x => x.AddPerformanceLoggingMiddleware());
```

### Custom Middleware

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

## HTTP Client Generation

Generate HTTP client handlers from contracts:

```csharp
using Shiny.Mediator.Http;

[Get("/api/users/{Id}")]
public class GetUserRequest : IRequest<UserDto>
{
    public int Id { get; set; }
    
    [Query]
    public string? Filter { get; set; }
    
    [Header("Authorization")]
    public string? AuthToken { get; set; }
}

[Post("/api/users")]
public class CreateUserRequest : IRequest<UserDto>
{
    [Body]
    public CreateUserPayload Payload { get; set; }
}

// Setup
builder.Services.AddHttpClient("api", client => 
    client.BaseAddress = new Uri("https://api.example.com"));

builder.AddShinyMediator(x => x.AddHttpClientServices());
```

## ASP.NET Endpoint Mapping

Map handlers directly to HTTP endpoints:

```csharp
[MediatorScoped]
[MediatorHttpGroup("/api/users")]
public class UserRequestHandler : IRequestHandler<GetUserRequest, UserDto>
{
    [MediatorHttpGet("/{id}")]
    public Task<UserDto> Handle(GetUserRequest request, IMediatorContext context, CancellationToken ct)
    {
        return Task.FromResult(new UserDto(request.Id, "John"));
    }
}

// In Program.cs
app.MapGeneratedMediatorEndpoints();
```

### Endpoint Attributes

```csharp
[MediatorHttpGroup("/api/admin", RequiresAuthorization = true)]
public class AdminHandler : ICommandHandler<AdminCommand>
{
    [MediatorHttpPost("/action", 
        OperationId = "AdminAction",
        Tags = new[] { "Admin" },
        Description = "Performs admin action",
        RequiresAuthorization = true,
        AuthorizationPolicies = new[] { "AdminPolicy" })]
    public Task Handle(AdminCommand command, IMediatorContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
```

## Server-Sent Events (ASP.NET)

```csharp
// Stream endpoint
app.MapGet("/sse", (IMediator mediator, TickerRequest request) => 
    mediator.Request(request));

// Event stream endpoint
app.MapGet("/events", (IMediator mediator) => 
    TypedResults.ServerSentEvents(mediator.EventStream<MyEvent>()));
```

## Scheduled Commands

```csharp
// Setup
builder.AddShinyMediator(x => x.AddInMemoryCommandScheduling());

// Scheduled command
public record SendReminderCommand(string UserId, string Message) : IScheduledCommand
{
    public DateTimeOffset? DueAt { get; set; }
}

// Schedule a command
var command = new SendReminderCommand("user1", "Don't forget!")
{
    DueAt = DateTimeOffset.Now.AddHours(1)
};
await mediator.Send(command);
```

## Contract Keys (Cache/Offline Key Generation)

```csharp
public class MyRequest : IRequest<MyData>, IContractKey
{
    public int UserId { get; set; }
    public string Region { get; set; }
    
    // Custom key for caching/offline storage
    public string GetKey() => $"user_{UserId}_{Region}";
}
```

## Flushing Stores (Cache/Offline)

```csharp
// Flush all caches and offline stores
await mediator.FlushAllStores();

// Flush specific keys
await mediator.FlushStores("user_", partialMatch: true);

// Or via events
await mediator.Publish(new FlushAllStoresEvent());
await mediator.Publish(new FlushStoresEvent("user_123", partialMatch: false));
```

## Mediator Context

Access runtime context within handlers:

```csharp
public Task<MyResult> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
{
    // Access service provider
    var service = context.ServiceScope.ServiceProvider.GetService<IMyService>();
    
    // Access headers set by middleware
    var cacheInfo = context.Cache();
    var offlineInfo = context.Offline();
    
    // Chain requests/commands within same scope
    var otherResult = await context.Request(new OtherRequest());
    await context.Send(new OtherCommand());
    await context.Publish(new SomeEvent());
    
    // Bypass middleware for this context
    context.BypassMiddlewareEnabled = true;
    context.BypassExceptionHandlingEnabled = true;
    
    // Access parent context (if chained)
    var parent = context.Parent;
    
    return Task.FromResult(new MyResult());
}
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
            // Log and swallow
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

## Testing

```csharp
// Use the testing package
dotnet add package Shiny.Mediator.Testing

// Mock mediator in tests
var mockMediator = new Mock<IMediator>();
mockMediator
    .Setup(x => x.Request(It.IsAny<GetUserRequest>(), It.IsAny<CancellationToken>(), null))
    .ReturnsAsync((new MockContext(), new UserDto(1, "Test")));
```

## Best Practices

1. **Use Records for Contracts**: Immutable and provide value equality
   ```csharp
   public record GetUserRequest(int UserId) : IRequest<UserDto>;
   ```

2. **One Handler Per File**: Keep handlers focused and testable

3. **Use Appropriate Lifetime**: 
   - `[MediatorSingleton]` for stateless handlers
   - `[MediatorScoped]` for handlers needing per-request services

4. **Leverage Middleware**: Don't repeat cross-cutting concerns in handlers

5. **Use Contract Keys**: For proper cache/offline key generation
   ```csharp
   public record MyRequest(int Id) : IRequest<MyData>, IContractKey
   {
       public string GetKey() => $"myrequest_{Id}";
   }
   ```

6. **Chain Requests via Context**: Use `context.Request()` instead of injecting `IMediator` in handlers

7. **Handle Cancellation**: Always pass and respect `CancellationToken`

## NuGet Packages Reference

| Package | Description |
|---------|-------------|
| `Shiny.Mediator` | Core mediator |
| `Shiny.Mediator.Contracts` | Contract interfaces only |
| `Shiny.Mediator.Maui` | MAUI integration |
| `Shiny.Mediator.Blazor` | Blazor integration |
| `Shiny.Mediator.AspNet` | ASP.NET integration |
| `Shiny.Mediator.Uno` | Uno Platform integration |
| `Shiny.Mediator.Prism` | Prism MVVM integration |
| `Shiny.Mediator.AppSupport` | Offline, replay, user notifications |
| `Shiny.Mediator.Resilience` | Polly resilience middleware |
| `Shiny.Mediator.FluentValidation` | FluentValidation middleware |
| `Shiny.Mediator.Caching.MicrosoftMemoryCache` | Memory caching |
| `Shiny.Mediator.DapperRequests` | Dapper SQL query handlers |
| `Shiny.Mediator.Sentry` | Sentry error tracking |
| `Shiny.Mediator.Testing` | Testing utilities |

