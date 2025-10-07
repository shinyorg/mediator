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