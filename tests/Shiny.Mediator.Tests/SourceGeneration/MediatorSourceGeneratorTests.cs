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

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
public class Handler1 : IRequestHandler<Request1, Response1>
{
    public Task<Response1> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response1(request.Data));
    }
}

[MediatorScoped]
public class Handler2 : IRequestHandler<Request2, Response2>
{
    public Task<Response2> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response2(request.Value * 2));
    }
}

[MediatorSingleton]
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

[MediatorSingleton]
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

[MediatorSingleton]
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

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
public class GetDataHandler : IRequestHandler<GetDataRequest, DataResponse>
{
    public Task<DataResponse> Handle(GetDataRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new DataResponse(request.Id, ""Data""));
    }
}

[MediatorScoped]
public class UpdateDataCommandHandler : ICommandHandler<UpdateDataCommand>
{
    public Task Handle(UpdateDataCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
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

[MediatorScoped]
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

[MediatorSingleton]
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

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(""Data""));
    }
}

[MediatorScoped]
public class MyCommandHandler : ICommandHandler<MyCommand>
{
    public Task Handle(MyCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

[MediatorSingleton]
public class LoggingRequestMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, IMediatorContext context, RequestMiddlewareDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        return next(request, context, cancellationToken);
    }
}

[MediatorScoped]
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

    [Fact]
    public Task SingleHandler_MultipleRequestInterfaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.MultiHandlers;

public record Request1(string Name) : IRequest<Response1>;
public record Response1(string Message);

public record Request2(int Value) : IRequest<Response2>;
public record Response2(int Result);

[MediatorSingleton]
public class MultiRequestHandler : IRequestHandler<Request1, Response1>, IRequestHandler<Request2, Response2>
{
    public Task<Response1> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response1($""Hello, {request.Name}!""));
    }

    public Task<Response2> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response2(request.Value * 2));
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task SingleHandler_MultipleStreamInterfaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.MultiStreamHandlers;

public record StreamRequest1(int Count) : IStreamRequest<string>;
public record StreamRequest2(int Max) : IStreamRequest<int>;

[MediatorScoped]
public class MultiStreamHandler : IStreamRequestHandler<StreamRequest1, string>, IStreamRequestHandler<StreamRequest2, int>
{
    public async IAsyncEnumerable<string> Handle(StreamRequest1 request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $""Item {i}"";
            await Task.Delay(10, cancellationToken);
        }
    }

    public async IAsyncEnumerable<int> Handle(StreamRequest2 request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Max; i++)
        {
            yield return i;
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
    public Task SingleHandler_MixedRequestAndStreamInterfaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.MixedHandlers;

public record GetDataRequest(int Id) : IRequest<DataResponse>;
public record DataResponse(int Id, string Data);

public record StreamDataRequest(int StartId) : IStreamRequest<DataItem>;
public record DataItem(int Id, string Value);

[MediatorSingleton]
public class DataHandler : IRequestHandler<GetDataRequest, DataResponse>, IStreamRequestHandler<StreamDataRequest, DataItem>
{
    public Task<DataResponse> Handle(GetDataRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new DataResponse(request.Id, $""Data for {request.Id}""));
    }

    public async IAsyncEnumerable<DataItem> Handle(StreamDataRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = request.StartId; i < request.StartId + 5; i++)
        {
            yield return new DataItem(i, $""Value {i}"");
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
    public Task SingleHandler_RequestStreamAndCommandInterfaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.ComplexHandlers;

public record GetUserRequest(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name);

public record StreamUsersRequest : IStreamRequest<UserDto>;

public record UpdateUserCommand(int UserId, string NewName) : ICommand;

[MediatorScoped]
public class UserHandler : IRequestHandler<GetUserRequest, UserDto>, 
                           IStreamRequestHandler<StreamUsersRequest, UserDto>,
                           ICommandHandler<UpdateUserCommand>
{
    public Task<UserDto> Handle(GetUserRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UserDto(request.UserId, ""John Doe""));
    }

    public async IAsyncEnumerable<UserDto> Handle(StreamUsersRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new UserDto(1, ""User 1"");
        await Task.Delay(10, cancellationToken);
        yield return new UserDto(2, ""User 2"");
    }

    public Task Handle(UpdateUserCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    [Fact]
    public Task MultipleHandlers_SomeWithMultipleInterfaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Combined;

public record Request1(string Data) : IRequest<Response1>;
public record Response1(string Result);

public record Request2(int Value) : IRequest<Response2>;
public record Response2(int Result);

public record Request3(bool Flag) : IRequest<Response3>;
public record Response3(bool Success);

public record StreamRequest1(int Count) : IStreamRequest<string>;

// Single interface handler
[MediatorSingleton]
public class Handler1 : IRequestHandler<Request1, Response1>
{
    public Task<Response1> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response1(request.Data));
    }
}

// Multiple interface handler
[MediatorScoped]
public class Handler2 : IRequestHandler<Request2, Response2>, IRequestHandler<Request3, Response3>
{
    public Task<Response2> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response2(request.Value * 2));
    }

    public Task<Response3> Handle(Request3 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response3(request.Flag));
    }
}

// Stream handler
[MediatorSingleton]
public class StreamHandler : IStreamRequestHandler<StreamRequest1, string>
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
    public Task SingleHandler_ThreeRequestInterfaces_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.TripleHandlers;

public record Request1(string A) : IRequest<string>;
public record Request2(int B) : IRequest<int>;
public record Request3(bool C) : IRequest<bool>;

[MediatorSingleton]
public class TripleHandler : IRequestHandler<Request1, string>, 
                              IRequestHandler<Request2, int>, 
                              IRequestHandler<Request3, bool>
{
    public Task<string> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.A.ToUpper());
    }

    public Task<int> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.B * 10);
    }

    public Task<bool> Handle(Request3 request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(!request.C);
    }
}
");
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);
        return Verify(result);
    }

    #region MSBuild Variable Tests

    [Fact]
    public Task CustomRegistrationClassName_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyRequest(string Data) : IRequest<MyResponse>;
public record MyResponse(string Result);

[MediatorSingleton]
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(request.Data));
    }
}
", registrationClassName: "MyCustomExtensions");
        
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        
        // Verify the custom registration class name is used
        var extensionFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_Extensions.g.cs"));
        extensionFile.ShouldNotBeNull();
        extensionFile.ToString().ShouldContain("public static class MyCustomExtensions");
        
        return Verify(result);
    }

    [Fact]
    public Task CustomRequestExecutorClassName_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyRequest(string Data) : IRequest<MyResponse>;
public record MyResponse(string Result);

[MediatorSingleton]
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(request.Data));
    }
}
", 
            rootNamespace: "MyApp",
            requestExecutorClassName: "MyCustomRequestExecutor");
        
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        
        // Verify the custom executor class name is used
        var executorFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_RequestExecutor.g.cs"));
        executorFile.ShouldNotBeNull();
        executorFile.ToString().ShouldContain("internal class MyCustomRequestExecutor");
        
        // Verify it's registered with the custom name
        var extensionFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_Extensions.g.cs"));
        extensionFile.ShouldNotBeNull();
        extensionFile.ToString().ShouldContain("global::MyApp.MyCustomRequestExecutor");
        
        return Verify(result);
    }

    [Fact]
    public Task CustomStreamRequestExecutorClassName_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyStreamRequest(int Count) : IStreamRequest<string>;

[MediatorSingleton]
public class MyStreamHandler : IStreamRequestHandler<MyStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(MyStreamRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $""Item {i}"";
            await Task.Delay(10, cancellationToken);
        }
    }
}
", 
            rootNamespace: "MyApp",
            streamRequestExecutorClassName: "MyCustomStreamExecutor");
        
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        
        // Verify the custom stream executor class name is used
        var streamExecutorFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_StreamRequestExecutor.g.cs"));
        streamExecutorFile.ShouldNotBeNull();
        streamExecutorFile.ToString().ShouldContain("internal class MyCustomStreamExecutor");
        
        // Verify it's registered with the custom name
        var extensionFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_Extensions.g.cs"));
        extensionFile.ShouldNotBeNull();
        extensionFile.ToString().ShouldContain("global::MyApp.MyCustomStreamExecutor");
        
        return Verify(result);
    }

    [Fact]
    public Task AllThreeCustomClassNames_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyRequest(string Data) : IRequest<MyResponse>;
public record MyResponse(string Result);

public record MyStreamRequest(int Count) : IStreamRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(request.Data));
    }
}

[MediatorSingleton]
public class MyStreamHandler : IStreamRequestHandler<MyStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(MyStreamRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $""Item {i}"";
            await Task.Delay(10, cancellationToken);
        }
    }
}
", 
            rootNamespace: "MyApp",
            registrationClassName: "MyCustomExtensions",
            requestExecutorClassName: "MyCustomRequestExecutor",
            streamRequestExecutorClassName: "MyCustomStreamExecutor");
        
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        
        // Verify all three custom class names are used
        var extensionFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_Extensions.g.cs"));
        extensionFile.ShouldNotBeNull();
        var extensionCode = extensionFile.ToString();
        extensionCode.ShouldContain("public static class MyCustomExtensions");
        extensionCode.ShouldContain("global::MyApp.MyCustomRequestExecutor");
        extensionCode.ShouldContain("global::MyApp.MyCustomStreamExecutor");
        
        var executorFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_RequestExecutor.g.cs"));
        executorFile.ShouldNotBeNull();
        executorFile.ToString().ShouldContain("internal class MyCustomRequestExecutor");
        
        var streamExecutorFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_StreamRequestExecutor.g.cs"));
        streamExecutorFile.ShouldNotBeNull();
        streamExecutorFile.ToString().ShouldContain("internal class MyCustomStreamExecutor");
        
        return Verify(result);
    }

    [Fact]
    public void CustomClassNames_WithNullOrEmpty_UsesDefaults()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp;

public record MyRequest(string Data) : IRequest<MyResponse>;
public record MyResponse(string Result);

[MediatorSingleton]
public class MyHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(request.Data));
    }
}
", 
            rootNamespace: "MyApp",
            registrationClassName: "",  // Empty string should use default
            requestExecutorClassName: "",  // Empty string should use default
            streamRequestExecutorClassName: "");  // Empty string should use default
        
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        
        // Verify default names are used when empty strings are provided
        var extensionFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_Extensions.g.cs"));
        extensionFile.ShouldNotBeNull();
        var extensionCode = extensionFile.ToString();
        extensionCode.ShouldContain("public static class __ShinyMediatorSourceGenExtensions");  // Default registration class name
        extensionCode.ShouldContain("global::MyApp.MyAppRequestExecutor");  // Default request executor
        
        var executorFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_RequestExecutor.g.cs"));
        executorFile.ShouldNotBeNull();
        executorFile.ToString().ShouldContain("internal class MyAppRequestExecutor");  // Default based on namespace
    }

    [Fact]
    public Task CustomClassNames_WithDifferentNamespace_GeneratesCorrectly()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyCompany.MyProduct.Features;

public record MyRequest(string Data) : IRequest<MyResponse>;
public record MyResponse(string Result);

public record MyStreamRequest(int Count) : IStreamRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MyResponse(request.Data));
    }
}

[MediatorSingleton]
public class MyStreamHandler : IStreamRequestHandler<MyStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(MyStreamRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return $""Item {i}"";
            await Task.Delay(10, cancellationToken);
        }
    }
}
", 
            rootNamespace: "MyCompany.MyProduct.Features",
            registrationClassName: "MediatorServiceExtensions",
            requestExecutorClassName: "CustomRequestExec",
            streamRequestExecutorClassName: "CustomStreamExec");
        
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        
        // Verify custom names work with different namespace
        var extensionFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("_Extensions.g.cs"));
        extensionFile.ShouldNotBeNull();
        var extensionCode = extensionFile.ToString();
        extensionCode.ShouldContain("namespace MyCompany.MyProduct.Features;");
        extensionCode.ShouldContain("public static class MediatorServiceExtensions");
        extensionCode.ShouldContain("global::MyCompany.MyProduct.Features.CustomRequestExec");
        extensionCode.ShouldContain("global::MyCompany.MyProduct.Features.CustomStreamExec");
        
        return Verify(result);
    }

    #endregion

    static GeneratorDriver BuildDriver(
        string sourceCode, 
        string? rootNamespace = "TestAssembly",
        string? registrationClassName = null,
        string? requestExecutorClassName = null,
        string? streamRequestExecutorClassName = null)
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
        
        // Setup analyzer config options with MSBuild properties
        var buildProperties = new Dictionary<string, string>();
        
        if (rootNamespace != null)
            buildProperties["build_property.RootNamespace"] = rootNamespace;
            
        if (registrationClassName != null)
            buildProperties["build_property.ShinyRegistrationClassName"] = registrationClassName;
            
        if (requestExecutorClassName != null)
            buildProperties["build_property.ShinyRequestExecutorClassName"] = requestExecutorClassName;
            
        if (streamRequestExecutorClassName != null)
            buildProperties["build_property.ShinyStreamRequestExecutorClassName"] = streamRequestExecutorClassName;
        
        var optionsProvider = new MockAnalyzerConfigOptionsProvider(buildProperties);
        
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            optionsProvider: optionsProvider);
            
        return driver.RunGenerators(compilation);
    }
}
