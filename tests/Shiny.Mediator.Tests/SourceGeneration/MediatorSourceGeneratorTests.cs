using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class MediatorSourceGeneratorTests
{
    [Fact]
    public Task GeneratesRegistry_WithSingletonRequestHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult($""Hello {request.Name}"");
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithScopedRequestHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(int Id) : IRequest<int>;

[MediatorScoped]
public class MyRequestHandler : IRequestHandler<MyRequest, int>
{
    public Task<int> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Id * 2);
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithMultipleHandlers()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record Request1(string Name) : IRequest<string>;
public record Request2(int Value) : IRequest<int>;
public record Command1(string Data) : ICommand;

[MediatorSingleton]
public class Request1Handler : IRequestHandler<Request1, string>
{
    public Task<string> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorSingleton]
public class Request2Handler : IRequestHandler<Request2, int>
{
    public Task<int> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

[MediatorScoped]
public class Command1Handler : ICommandHandler<Command1>
{
    public Task Handle(Command1 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithEventHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyEvent(string Message) : IEvent;

[MediatorSingleton]
public class MyEventHandler : IEventHandler<MyEvent>
{
    public Task Handle(MyEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithStreamRequestHandler()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace TestApp;

public record StreamRequest(int Count) : IStreamRequest<int>;

[MediatorSingleton]
public class StreamRequestHandler : IStreamRequestHandler<StreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(StreamRequest request, IMediatorContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return i;
        }
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithRequestMiddleware()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;
public record MyResponse(string Result);

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorSingleton]
public class MyRequestMiddleware : IRequestMiddleware<MyRequest, string>
{
    public async Task<string> Process(IMediatorContext context, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var result = await next();
        return result.ToUpper();
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithOpenGenericMiddleware()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorSingleton]
public class GenericStreamMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult>
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(IMediatorContext context, StreamRequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        return next();
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithCustomRootNamespace()
    {
        var driver = BuildDriverWithOptions(
            @"
using Shiny.Mediator;


public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}",
            rootNamespace: "CustomNamespace");
        
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithPublicAccessModifier()
    {
        var driver = BuildDriverWithOptions(
            @"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}",
            accessModifier: "public");
        
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithCustomMethodName()
    {
        var driver = BuildDriverWithOptions(
            @"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

[MediatorSingleton]
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}",
            methodName: "AddCustomMediatorServices");
        
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithMixedLifetimes()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record Request1(string Name) : IRequest<string>;
public record Request2(int Value) : IRequest<int>;

[MediatorSingleton]
public class Request1Handler : IRequestHandler<Request1, string>
{
    public Task<string> Handle(Request1 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}

[MediatorScoped]
public class Request2Handler : IRequestHandler<Request2, int>
{
    public Task<int> Handle(Request2 request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

//     [Fact]
//     public Task DoesNotGenerate_WithoutAttribute()
//     {
//         var driver = BuildDriver(@"
// using Shiny.Mediator;
//
// namespace TestApp;
//
// public record MyRequest(string Name) : IRequest<string>;
//
// [MediatorSingleton]
// public class MyRequestHandler : IRequestHandler<MyRequest, string>
// {
//     public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
//         => Task.FromResult(request.Name);
// }");
//         var result = driver.GetRunResult().Results.FirstOrDefault();
//         result.Exception.ShouldBeNull();
//         // Should only generate the attributes file, no registry
//         result.GeneratedSources.Length.ShouldBe(1);
//         result.GeneratedSources[0].HintName.ShouldBe("MediatorAttributes.g.cs");
//         return Verify(result);
//     }

    [Fact]
    public Task DoesNotGenerate_WithoutMediatorAttribute()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyRequest(string Name) : IRequest<string>;

// No [MediatorSingleton] or [MediatorScoped] attribute
public class MyRequestHandler : IRequestHandler<MyRequest, string>
{
    public Task<string> Handle(MyRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Name);
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        // Should only generate the attributes file, no registry
        result.GeneratedSources.Length.ShouldBe(1);
        result.GeneratedSources[0].HintName.ShouldBe("MediatorAttributes.g.cs");
        return Verify(result);
    }

    [Fact]
    public Task GeneratesRegistry_WithCommandMiddleware()
    {
        var driver = BuildDriver(@"
using Shiny.Mediator;


namespace TestApp;

public record MyCommand(string Data) : ICommand;

[MediatorSingleton]
public class MyCommandHandler : ICommandHandler<MyCommand>
{
    public Task Handle(MyCommand request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

[MediatorSingleton]
public class MyCommandMiddleware : ICommandMiddleware<MyCommand>
{
    public async Task Process(IMediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        await next();
    }
}");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }

    static GeneratorDriver BuildDriver(string sourceCode)
        => BuildDriverWithOptions(sourceCode);

    static GeneratorDriver BuildDriverWithOptions(
        string sourceCode,
        string? rootNamespace = null,
        string? accessModifier = null,
        string? methodName = null)
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

        // Add MSBuild options if specified
        if (rootNamespace != null || accessModifier != null || methodName != null)
        {
            var optionsProvider = new TestAnalyzerConfigOptionsProvider(
                rootNamespace,
                accessModifier,
                methodName);
            driver = (CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);
        }

        return driver.RunGenerators(compilation);
    }

    class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions globalOptions;

        public TestAnalyzerConfigOptionsProvider(
            string? rootNamespace,
            string? accessModifier,
            string? methodName)
        {
            this.globalOptions = new TestAnalyzerConfigOptions(
                rootNamespace,
                accessModifier,
                methodName);
        }

        public override AnalyzerConfigOptions GlobalOptions => this.globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.globalOptions;
    }

    class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> options = new();

        public TestAnalyzerConfigOptions(
            string? rootNamespace,
            string? accessModifier,
            string? methodName)
        {
            if (rootNamespace != null)
                this.options["build_property.RootNamespace"] = rootNamespace;
            if (accessModifier != null)
                this.options["build_property.ShinyMediatorRegistryAccessModifier"] = accessModifier;
            if (methodName != null)
                this.options["build_property.ShinyMediatorRegistryMethodName"] = methodName;
        }

        public override bool TryGetValue(string key, out string value)
            => this.options.TryGetValue(key, out value!);
    }
}