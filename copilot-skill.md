# Shiny Mediator - GitHub Copilot Instructions

This repository contains **Shiny Mediator**, a mediator pattern implementation for .NET applications. When working with this codebase, follow these patterns and conventions.

## Documentation

Full documentation: https://shinylib.net/mediator

## Project Structure

- `src/Shiny.Mediator` - Core mediator library
- `src/Shiny.Mediator.Contracts` - Contract interfaces only (IRequest, ICommand, IEvent)
- `src/Shiny.Mediator.Maui` - .NET MAUI integration
- `src/Shiny.Mediator.Blazor` - Blazor integration
- `src/Shiny.Mediator.AspNet` - ASP.NET integration
- `src/Shiny.Mediator.AppSupport` - Offline, replay, user notifications middleware
- `src/Shiny.Mediator.Resilience` - Polly resilience middleware
- `src/Shiny.Mediator.SourceGenerators` - Source generators for DI registration
- `samples/` - Sample applications
- `tests/` - Unit tests

## Core Concepts

### Contract Types

```csharp
// Request with result (query pattern)
public record MyRequest(int Id) : IRequest<MyResult>;

// Command (void operation)
public record MyCommand(string Data) : ICommand;

// Event (pub/sub)
public record MyEvent(string Message) : IEvent;

// Stream request (IAsyncEnumerable)
public record MyStreamRequest(int Count) : IStreamRequest<string>;
```

### Handler Interfaces

```csharp
// Request handler
public class MyRequestHandler : IRequestHandler<MyRequest, MyResult>
{
    public Task<MyResult> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResult());
    }
}

// Command handler
public class MyCommandHandler : ICommandHandler<MyCommand>
{
    public Task Handle(MyCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Event handler
public class MyEventHandler : IEventHandler<MyEvent>
{
    public Task Handle(MyEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// Stream request handler
public class MyStreamHandler : IStreamRequestHandler<MyStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(
        MyStreamRequest request,
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $"Item {i}";
        }
    }
}
```

### Handler Registration Attributes

Always use source-generated attributes for DI registration:

```csharp
[MediatorSingleton]  // Singleton lifetime
public class MySingletonHandler : IRequestHandler<MyRequest, string> { }

[MediatorScoped]     // Scoped lifetime (per-request)
public class MyScopedHandler : ICommandHandler<MyCommand> { }
```

### Middleware Attributes

Apply to handler methods:

```csharp
[MediatorSingleton]
public class MyHandler : IRequestHandler<MyRequest, MyData>
{
    [Cache(AbsoluteExpirationSeconds = 300)]  // Cache result
    [OfflineAvailable]                         // Store for offline
    [Resilient("policyName")]                  // Apply resilience
    [MainThread]                               // Execute on main thread (MAUI)
    [TimerRefresh(5000)]                       // Auto-refresh streams
    public Task<MyData> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
    {
        return Task.FromResult(new MyData());
    }
}
```

### Validation

```csharp
[Validate]  // Enable data annotation validation
public class CreateUserCommand : ICommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
```

## Mediator Usage

### Sending Requests/Commands/Events

```csharp
public class MyService(IMediator mediator)
{
    public async Task DoWork()
    {
        // Request with result
        var response = await mediator.Request(new MyRequest(1));
        var result = response.Result;
        var context = response.Context;

        // Command (no result)
        await mediator.Send(new MyCommand("data"));

        // Publish event
        await mediator.Publish(new MyEvent("message"));
        
        // Fire and forget
        mediator.PublishToBackground(new MyEvent("background"));

        // Stream request
        await foreach (var (ctx, item) in mediator.Request(new MyStreamRequest(10)))
        {
            Console.WriteLine(item);
        }
    }
}
```

### Context Usage in Handlers

```csharp
public async Task<MyResult> Handle(MyRequest request, IMediatorContext context, CancellationToken ct)
{
    // Access services
    var service = context.ServiceScope.ServiceProvider.GetService<IMyService>();
    
    // Chain requests within same scope
    var other = await context.Request(new OtherRequest());
    await context.Send(new OtherCommand());
    await context.Publish(new SomeEvent());
    
    // Check middleware context
    var cacheInfo = context.Cache();
    var offlineInfo = context.Offline();
    
    return new MyResult();
}
```

## Setup Patterns

### ASP.NET

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator(x => x
    .AddMediatorRegistry()
    .AddDataAnnotations()
    .AddResiliencyMiddleware(builder.Configuration)
);

var app = builder.Build();
app.MapGeneratedMediatorEndpoints();
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
        .PreventEventExceptions()
        .AddDataAnnotations()
        .AddMauiPersistentCache()
        .AddStandardAppSupportMiddleware()
    );
    
    return builder.Build();
}
```

## ASP.NET Endpoint Mapping

```csharp
[MediatorScoped]
[MediatorHttpGroup("/api/users")]
public class UserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    [MediatorHttpGet("/{id}")]
    public Task<UserDto> Handle(GetUserRequest request, IMediatorContext context, CancellationToken ct)
    {
        return Task.FromResult(new UserDto(request.Id, "John"));
    }
}
```

## HTTP Client Generation

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
```

## Middleware Interfaces

```csharp
// Request middleware
public class MyMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Before
        var result = await next();
        // After
        return result;
    }
}

// Command middleware
public class MyCommandMiddleware<TCommand> : ICommandMiddleware<TCommand>
    where TCommand : ICommand
{
    public async Task Process(
        IMediatorContext context,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        // Before
        await next();
        // After
    }
}

// Event middleware
public class MyEventMiddleware<TEvent> : IEventMiddleware<TEvent>
    where TEvent : IEvent
{
    public async Task Process(
        IMediatorContext context,
        EventHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        // Before
        await next();
        // After
    }
}

// Stream middleware
public class MyStreamMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult>
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        IMediatorContext context,
        StreamHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        return next();
    }
}
```

## Contract Keys for Caching

```csharp
public record MyRequest(int UserId, string Region) : IRequest<MyData>, IContractKey
{
    public string GetKey() => $"user_{UserId}_{Region}";
}
```

## Exception Handling

```csharp
public class MyExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        // Return true if handled, false to rethrow
        return Task.FromResult(false);
    }
}

// Register
builder.AddShinyMediator(x => x
    .AddExceptionHandler<MyExceptionHandler>()
    .PreventEventExceptions()
);
```

## Code Conventions

1. Use `record` types for contracts (immutable, value equality)
2. Use `[MediatorSingleton]` for stateless handlers, `[MediatorScoped]` for stateful
3. Always pass `CancellationToken` through the call chain
4. Use `context.Request()` to chain requests within handlers
5. Apply middleware attributes to handler methods, not classes
6. Implement `IContractKey` for custom cache/offline keys
7. Use `partial` classes when using source generators that require it

## Testing

```csharp
// Mock the mediator
var mockMediator = new Mock<IMediator>();
mockMediator
    .Setup(x => x.Request(It.IsAny<GetUserRequest>(), It.IsAny<CancellationToken>(), null))
    .ReturnsAsync((new MockContext(), new UserDto(1, "Test")));
```

