using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class MediatorSourceGeneratorTests
{
    [Fact]
    public Task SingletonRequestHandler_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyNamespace.Handlers;

public record MyRequest(string Name) : IRequest<MyResponse>;

public record MyResponse
{
    public string Message { get; set; }
}

[SingletonMediatorHandler]
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var response = new MyResponse { Message = $""Hello, {request.Name}!"" };
        return Task.FromResult(response);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task ScopedRequestHandler_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Handlers;

public record GetUserRequest(int UserId) : IRequest<UserResponse>;

public record UserResponse(int Id, string Name, string Email);

[ScopedMediatorHandler]
public class GetUserRequestHandler : IRequestHandler<GetUserRequest, UserResponse>
{
    public Task<UserResponse> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UserResponse(request.UserId, ""John"", ""john@example.com""));
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task SingletonStreamHandler_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Streams;

public record TickerRequest(int Count, int Interval) : IStreamRequest<string>;

[SingletonMediatorHandler]
public class TickerRequestHandler : IStreamRequestHandler<TickerRequest, string>
{
    public async IAsyncEnumerable<string> Handle(TickerRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $""Tick {i + 1}"";
            await Task.Delay(request.Interval, cancellationToken);
        }
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task ScopedStreamHandler_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.EventStreams;

public record EventStreamRequest(string EventType) : IStreamRequest<EventData>;

public record EventData(string Type, string Payload, System.DateTime Timestamp);

[ScopedMediatorHandler]
public class EventStreamHandler : IStreamRequestHandler<EventStreamRequest, EventData>
{
    public async IAsyncEnumerable<EventData> Handle(EventStreamRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new EventData(request.EventType, ""Payload1"", System.DateTime.Now);
        await Task.Delay(100, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task MultipleHandlers_MixedLifetimes_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record Request1(string Data) : IRequest<Response1>;
public record Response1(string Result);

public record Request2(int Value) : IRequest<Response2>;
public record Response2(int Result);

public record StreamRequest1(int Count) : IStreamRequest<string>;

[SingletonMediatorHandler]
public class Handler1 : IRequestHandler<Request1, Response1>
{
    public Task<Response1> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response1(request.Data));
    }
}

[ScopedMediatorHandler]
public class Handler2 : IRequestHandler<Request2, Response2>
{
    public Task<Response2> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response2(request.Value * 2));
    }
}

[SingletonMediatorHandler]
public class StreamHandler1 : IStreamRequestHandler<StreamRequest1, string>
{
    public async IAsyncEnumerable<string> Handle(StreamRequest1 request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $""Item {i}"";
            await Task.Delay(10, cancellationToken);
        }
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task NoHandlers_GeneratesNothing()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyRequest(string Data) : IRequest<MyResponse>;
public record MyResponse(string Result);

// No handler with attributes
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(request.Data));
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        // Should only generate the attributes file
        result.GeneratedTrees.Length.ShouldBe(1);
        result.GeneratedTrees[0].FilePath.ShouldEndWith("MediatorAttributes.g.cs");
        return Verify(result);
    }

    [Fact]
    public Task ComplexNamespaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyCompany.MyProduct.Features.Users.Handlers;

public record CreateUserRequest(string Name, string Email) : IRequest<CreateUserResponse>;
public record CreateUserResponse(int UserId, string Name);

[SingletonMediatorHandler]
public class CreateUserRequestHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    public Task<CreateUserResponse> Handle(CreateUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateUserResponse(1, request.Name));
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task GenericResponseTypes_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record GetListRequest(int Count) : IRequest<List<string>>;

[SingletonMediatorHandler]
public class GetListRequestHandler : IRequestHandler<GetListRequest, List<string>>
{
    public Task<List<string>> Handle(GetListRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var list = new List<string>();
        for (int i = 0; i < request.Count; i++)
        {
            list.Add($""Item {i}"");
        }
        return Task.FromResult(list);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task MultipleHandlersInDifferentNamespaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Features.Users;

public record GetUserRequest(int Id) : IRequest<UserDto>;
public record UserDto(int Id, string Name);

[SingletonMediatorHandler]
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    public Task<UserDto> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UserDto(request.Id, ""User""));
    }
}

namespace MyApp.Features.Orders;

public record GetOrderRequest(int Id) : IRequest<OrderDto>;
public record OrderDto(int Id, decimal Total);

[ScopedMediatorHandler]
public class GetOrderHandler : IRequestHandler<GetOrderRequest, OrderDto>
{
    public Task<OrderDto> Handle(GetOrderRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new OrderDto(request.Id, 100.50m));
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task CommandHandler_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Commands;

public record CreateUserCommand(string Name, string Email) : ICommand;

[SingletonMediatorHandler]
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public Task Handle(CreateUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Create user logic here
        return Task.CompletedTask;
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        // Should generate attributes and extensions, but NO executors
        result.GeneratedTrees.Length.ShouldBe(2);
        return Verify(result);
    }

    [Fact]
    public Task EventHandler_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Events;

public record UserCreatedEvent(int UserId, string Name) : IEvent;

[ScopedMediatorHandler]
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Handle event logic here
        return Task.CompletedTask;
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        // Should generate attributes and extensions, but NO executors
        result.GeneratedTrees.Length.ShouldBe(2);
        return Verify(result);
    }

    [Fact]
    public Task MixedHandlers_RequestCommandEvent_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record GetDataRequest(int Id) : IRequest<DataResponse>;
public record DataResponse(int Id, string Data);

public record UpdateDataCommand(int Id, string NewData) : ICommand;

public record DataUpdatedEvent(int Id, string Data) : IEvent;

[SingletonMediatorHandler]
public class GetDataHandler : IRequestHandler<GetDataRequest, DataResponse>
{
    public Task<DataResponse> Handle(GetDataRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new DataResponse(request.Id, ""Data""));
    }
}

[ScopedMediatorHandler]
public class UpdateDataCommandHandler : ICommandHandler<UpdateDataCommand>
{
    public Task Handle(UpdateDataCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

[SingletonMediatorHandler]
public class DataUpdatedEventHandler : IEventHandler<DataUpdatedEvent>
{
    public Task Handle(DataUpdatedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        // Should generate attributes, request executor, and extensions (no stream executor)
        result.GeneratedTrees.Length.ShouldBe(3);
        return Verify(result);
    }

    [Fact]
    public Task ClosedCommandMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Middleware;

public record MyCommand(string Data) : ICommand;

[ScopedMediatorMiddleware]
public class MyCommandMiddleware : ICommandMiddleware<MyCommand>
{
    public Task Handle(MyCommand command, IMediatorContext context, CommandMiddlewareDelegate<MyCommand> next, CancellationToken cancellationToken)
    {
        return next(command, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task OpenGenericCommandMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Middleware;

[SingletonMediatorMiddleware]
public class ValidationCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public Task Handle(TCommand command, IMediatorContext context, CommandMiddlewareDelegate<TCommand> next, CancellationToken cancellationToken)
    {
        // Validation logic
        return next(command, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task ClosedRequestMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Middleware;

public record MyRequest(int Id) : IRequest<MyResponse>;
public record MyResponse(string Data);

[ScopedMediatorMiddleware]
public class MyRequestMiddleware : IRequestMiddleware<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, RequestMiddlewareDelegate<MyRequest, MyResponse> next, CancellationToken cancellationToken)
    {
        return next(request, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task OpenGenericRequestMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Middleware;

[SingletonMediatorMiddleware]
public class LoggingRequestMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, IMediatorContext context, RequestMiddlewareDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        // Logging logic
        return next(request, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task ClosedEventMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Middleware;

public record MyEvent(string Message) : IEvent;

[ScopedMediatorMiddleware]
public class MyEventMiddleware : IEventMiddleware<MyEvent>
{
    public Task Handle(MyEvent @event, IMediatorContext context, EventMiddlewareDelegate<MyEvent> next, CancellationToken cancellationToken)
    {
        return next(@event, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task OpenGenericEventMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Middleware;

[SingletonMediatorMiddleware]
public class AuditEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public Task Handle(TEvent @event, IMediatorContext context, EventMiddlewareDelegate<TEvent> next, CancellationToken cancellationToken)
    {
        // Audit logic
        return next(@event, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task MixedHandlersAndMiddleware_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyRequest(int Id) : IRequest<MyResponse>;
public record MyResponse(string Data);

public record MyCommand(string Action) : ICommand;

[SingletonMediatorHandler]
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(""Data""));
    }
}

[ScopedMediatorHandler]
public class MyCommandHandler : ICommandHandler<MyCommand>
{
    public Task Handle(MyCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

[SingletonMediatorMiddleware]
public class LoggingRequestMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, IMediatorContext context, RequestMiddlewareDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        return next(request, context, cancellationToken);
    }
}

[ScopedMediatorMiddleware]
public class ValidationCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public Task Handle(TCommand command, IMediatorContext context, CommandMiddlewareDelegate<TCommand> next, CancellationToken cancellationToken)
    {
        return next(command, context, cancellationToken);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    static GeneratorDriver BuildDriver(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        
        // Add minimal references for compilation
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(IMediator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], references, options);

        var generator = new MediatorSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }
}

